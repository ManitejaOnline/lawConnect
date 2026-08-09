using FluentValidation;
using LawConnect.Application.Common.Interfaces;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Common.Realtime;
using LawConnect.Application.Common.Services;
using LawConnect.Application.Features.Emergency.Models;
using LawConnect.Application.Features.Emergency.Services;
using LawConnect.Application.Features.Lawyers.Services;
using LawConnect.Application.Features.Payments.Services;
using LawConnect.Domain.Entities;
using LawConnect.Domain.Enums;
using LawConnect.Domain.Exceptions;
using LawConnect.Shared.Constants;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawConnect.Application.Features.Emergency.Commands.AcceptEmergencyAssignment;

/// <summary>
/// Lawyer accepts an incoming SOS assignment (first accept wins).
/// </summary>
public sealed record AcceptEmergencyAssignmentCommand(Guid AssignmentId)
    : IRequest<Result<EmergencyRequestResponse>>;

/// <summary>
/// Validates <see cref="AcceptEmergencyAssignmentCommand"/>.
/// </summary>
public sealed class AcceptEmergencyAssignmentCommandValidator : AbstractValidator<AcceptEmergencyAssignmentCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AcceptEmergencyAssignmentCommandValidator"/> class.
    /// </summary>
    public AcceptEmergencyAssignmentCommandValidator() => RuleFor(x => x.AssignmentId).NotEmpty();
}

/// <summary>
/// Handles <see cref="AcceptEmergencyAssignmentCommand"/>.
/// </summary>
public sealed class AcceptEmergencyAssignmentCommandHandler
    : IRequestHandler<AcceptEmergencyAssignmentCommand, Result<EmergencyRequestResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentLawyerService _currentLawyer;
    private readonly IDateTimeProvider _clock;
    private readonly IEmergencyDispatchService _dispatch;
    private readonly IEmergencyRealtimePublisher _realtime;
    private readonly IEmergencyAcceptCoordinator _acceptCoordinator;
    private readonly IPaymentService _payments;
    private readonly IUserNotificationDispatcher _notifications;

    /// <summary>
    /// Initializes a new instance of the <see cref="AcceptEmergencyAssignmentCommandHandler"/> class.
    /// </summary>
    public AcceptEmergencyAssignmentCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentLawyerService currentLawyer,
        IDateTimeProvider clock,
        IEmergencyDispatchService dispatch,
        IEmergencyRealtimePublisher realtime,
        IEmergencyAcceptCoordinator acceptCoordinator,
        IPaymentService payments,
        IUserNotificationDispatcher notifications)
    {
        _dbContext = dbContext;
        _currentLawyer = currentLawyer;
        _clock = clock;
        _dispatch = dispatch;
        _realtime = realtime;
        _acceptCoordinator = acceptCoordinator;
        _payments = payments;
        _notifications = notifications;
    }

    /// <inheritdoc />
    public async Task<Result<EmergencyRequestResponse>> Handle(
        AcceptEmergencyAssignmentCommand request,
        CancellationToken cancellationToken)
    {
        var lawyer = await _currentLawyer.GetRequiredAsync(cancellationToken);
        LawyerOperationalRules.EnsureApproved(lawyer);

        var ownership = await _dbContext.EmergencyAssignments
            .AsNoTracking()
            .Select(a => new { a.Id, a.LawyerId, a.EmergencyRequestId })
            .FirstOrDefaultAsync(a => a.Id == request.AssignmentId, cancellationToken)
            ?? throw new NotFoundException(nameof(EmergencyAssignment), request.AssignmentId);

        if (ownership.LawyerId != lawyer.Id)
        {
            throw new ForbiddenException("You are not allowed to accept this SOS assignment.");
        }

        return await _acceptCoordinator.RunExclusiveAsync(
            ownership.EmergencyRequestId,
            () => ClaimAsync(request.AssignmentId, lawyer, cancellationToken),
            cancellationToken);
    }

    private async Task<Result<EmergencyRequestResponse>> ClaimAsync(
        Guid assignmentId,
        Lawyer lawyer,
        CancellationToken cancellationToken)
    {
        var assignment = await _dbContext.EmergencyAssignments
            .Include(a => a.EmergencyRequest)
                .ThenInclude(r => r.Category)
            .Include(a => a.EmergencyRequest)
                .ThenInclude(r => r.Customer)
            .Include(a => a.EmergencyRequest)
                .ThenInclude(r => r.Assignments)
                    .ThenInclude(a => a.Lawyer)
            .FirstOrDefaultAsync(a => a.Id == assignmentId, cancellationToken)
            ?? throw new NotFoundException(nameof(EmergencyAssignment), assignmentId);

        var sos = assignment.EmergencyRequest;

        if (assignment.Status != EmergencyAssignmentStatus.Pending)
        {
            throw new ConflictException("This SOS assignment is no longer pending.");
        }

        if (!EmergencyLifecycle.CanAccept(sos.Status) || sos.AssignedLawyerId is not null)
        {
            throw new ConflictException(
                sos.Status == EmergencyStatus.Cancelled
                    ? "This emergency was cancelled by the customer."
                    : EmergencyLifecycle.IsAssignedLike(sos.Status)
                        ? "This emergency has already been accepted by another lawyer."
                        : "This emergency is no longer active.");
        }

        var fee = lawyer.EmergencyFee ?? lawyer.ConsultationFee;
        if (fee <= 0)
        {
            throw new BusinessRuleException(
                "Lawyer emergency fee is not configured.",
                "Payment.EmergencyFeeMissing");
        }

        await _payments.PlaceEmergencyHoldAsync(
            sos.Id,
            sos.Customer.UserId,
            fee,
            $"emergency-hold:{sos.Id:D}",
            cancellationToken);

        var now = _clock.UtcNow;
        sos.Status = EmergencyStatus.Assigned;
        sos.AssignedLawyerId = lawyer.Id;
        sos.AcceptedAssignmentId = assignment.Id;
        sos.AssignedAtUtc = now;
        sos.SearchExpiresAtUtc = null;
        sos.EmergencyFeeSnapshot = fee;

        assignment.Status = EmergencyAssignmentStatus.Accepted;
        assignment.RespondedAtUtc = now;
        assignment.ResponseTimeSeconds = Math.Max(0, (now - assignment.NotifiedAtUtc).TotalSeconds);

        var losers = sos.Assignments
            .Where(a => a.Id != assignment.Id && a.Status == EmergencyAssignmentStatus.Pending)
            .ToList();

        foreach (var loser in losers)
        {
            loser.Status = EmergencyAssignmentStatus.Cancelled;
            loser.RespondedAtUtc = now;
            loser.CancellationReason = "This emergency has been accepted by another lawyer.";
        }

        _dispatch.AddTimeline(
            sos,
            EmergencyTimelineEventTypes.LawyerAccepted,
            "Lawyer accepted the SOS.",
            lawyer.UserId,
            new
            {
                LawyerId = lawyer.Id,
                AssignmentId = assignment.Id,
                ResponseTimeSeconds = assignment.ResponseTimeSeconds,
                EmergencyFee = fee
            });
        _dispatch.AddTimeline(
            sos,
            EmergencyTimelineEventTypes.AssignmentsCancelled,
            $"Cancelled {losers.Count} remaining assignment(s).",
            metadata: new { Count = losers.Count });

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException("Another lawyer already accepted this SOS.");
        }

        foreach (var loser in losers)
        {
            var reason = loser.CancellationReason ?? "This emergency has been accepted by another lawyer.";
            await _realtime.PublishAssignmentCancelledAsync(
                loser.Lawyer.UserId,
                sos.Id,
                loser.Id,
                reason,
                cancellationToken);
            await _notifications.NotifyUserAsync(
                loser.Lawyer.UserId,
                "Emergency update",
                reason,
                NotificationTypes.EmergencyTaken,
                new Dictionary<string, string>
                {
                    ["type"] = NotificationTypes.EmergencyTaken,
                    ["requestId"] = sos.Id.ToString("D"),
                    ["assignmentId"] = loser.Id.ToString("D"),
                    ["deepLink"] = "lawconnect://emergency/incoming"
                },
                cancellationToken);
        }

        await _notifications.NotifyUserAsync(
            lawyer.UserId,
            "Emergency assigned",
            $"You are assigned to the {sos.Category.Name} emergency.",
            NotificationTypes.EmergencyStatus,
            new Dictionary<string, string>
            {
                ["type"] = NotificationTypes.EmergencyStatus,
                ["screen"] = "LawyerEmergencyActive",
                ["requestId"] = sos.Id.ToString("D"),
                ["deepLink"] = $"lawconnect://emergency/active?requestId={sos.Id:D}"
            },
            cancellationToken);

        var response = _dispatch.ToResponse(sos, sos.Category.Name);
        await _realtime.PublishStatusAsync(response, cancellationToken);
        return Result.Success(response);
    }
}
