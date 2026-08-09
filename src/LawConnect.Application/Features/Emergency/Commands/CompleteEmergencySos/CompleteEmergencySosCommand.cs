using FluentValidation;
using LawConnect.Application.Common.Interfaces;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Common.Realtime;
using LawConnect.Application.Common.Services;
using LawConnect.Application.Features.Emergency.Models;
using LawConnect.Application.Features.Emergency.Services;
using LawConnect.Application.Features.Payments.Services;
using LawConnect.Domain.Entities;
using LawConnect.Domain.Enums;
using LawConnect.Domain.Exceptions;
using LawConnect.Shared.Constants;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawConnect.Application.Features.Emergency.Commands.CompleteEmergencySos;

/// <summary>
/// Marks an assigned SOS as completed and captures the emergency hold.
/// </summary>
public sealed record CompleteEmergencySosCommand(Guid RequestId) : IRequest<Result<EmergencyRequestResponse>>;

/// <summary>
/// Validates <see cref="CompleteEmergencySosCommand"/>.
/// </summary>
public sealed class CompleteEmergencySosCommandValidator : AbstractValidator<CompleteEmergencySosCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CompleteEmergencySosCommandValidator"/> class.
    /// </summary>
    public CompleteEmergencySosCommandValidator() => RuleFor(x => x.RequestId).NotEmpty();
}

/// <summary>
/// Handles <see cref="CompleteEmergencySosCommand"/>.
/// </summary>
public sealed class CompleteEmergencySosCommandHandler
    : IRequestHandler<CompleteEmergencySosCommand, Result<EmergencyRequestResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly ICurrentLawyerService _currentLawyer;
    private readonly IDateTimeProvider _clock;
    private readonly IEmergencyDispatchService _dispatch;
    private readonly IEmergencyRealtimePublisher _realtime;
    private readonly IPaymentService _payments;

    /// <summary>
    /// Initializes a new instance of the <see cref="CompleteEmergencySosCommandHandler"/> class.
    /// </summary>
    public CompleteEmergencySosCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        ICurrentLawyerService currentLawyer,
        IDateTimeProvider clock,
        IEmergencyDispatchService dispatch,
        IEmergencyRealtimePublisher realtime,
        IPaymentService payments)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _currentLawyer = currentLawyer;
        _clock = clock;
        _dispatch = dispatch;
        _realtime = realtime;
        _payments = payments;
    }

    /// <inheritdoc />
    public async Task<Result<EmergencyRequestResponse>> Handle(
        CompleteEmergencySosCommand request,
        CancellationToken cancellationToken)
    {
        var sos = await _dbContext.EmergencyRequests
            .Include(r => r.Category)
            .Include(r => r.Customer)
            .FirstOrDefaultAsync(r => r.Id == request.RequestId, cancellationToken)
            ?? throw new NotFoundException(nameof(EmergencyRequest), request.RequestId);

        if (!EmergencyLifecycle.CanComplete(sos.Status))
        {
            throw new BusinessRuleException(
                "Only assigned SOS requests can be completed.",
                "Emergency.InvalidStatus");
        }

        var userId = _currentUser.GetRequiredUserId();
        var isCustomer = sos.Customer.UserId == userId;
        var isAssignedLawyer = false;
        if (!isCustomer && sos.AssignedLawyerId.HasValue)
        {
            var lawyer = await _currentLawyer.GetRequiredAsync(cancellationToken);
            isAssignedLawyer = lawyer.Id == sos.AssignedLawyerId.Value;
        }

        if (!isCustomer && !isAssignedLawyer)
        {
            throw new ForbiddenException("You are not allowed to complete this SOS request.");
        }

        await _payments.CaptureEmergencyHoldAsync(
            sos.Id,
            $"emergency-capture:{sos.Id:D}",
            cancellationToken);

        sos.Status = EmergencyStatus.Completed;
        sos.CompletedAtUtc = _clock.UtcNow;
        sos.SearchExpiresAtUtc = null;
        _dispatch.AddTimeline(
            sos,
            EmergencyTimelineEventTypes.Completed,
            "Emergency SOS completed.",
            userId);
        await _dispatch.WriteHistoryAsync(sos, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var response = _dispatch.ToResponse(sos, sos.Category.Name);
        await _realtime.PublishStatusAsync(response, cancellationToken);
        return Result.Success(response);
    }
}
