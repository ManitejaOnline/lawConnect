using FluentValidation;
using LawConnect.Application.Common.Interfaces;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Common.Realtime;
using LawConnect.Application.Common.Services;
using LawConnect.Application.Features.Emergency.Services;
using LawConnect.Application.Features.Payments.Services;
using LawConnect.Domain.Entities;
using LawConnect.Domain.Enums;
using LawConnect.Domain.Exceptions;
using LawConnect.Shared.Constants;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawConnect.Application.Features.Emergency.Commands.CancelEmergencySos;

/// <summary>
/// Cancels an active customer SOS request and releases any payment hold.
/// </summary>
public sealed record CancelEmergencySosCommand(Guid RequestId, string? Reason) : IRequest<Result>;

/// <summary>
/// Validates <see cref="CancelEmergencySosCommand"/>.
/// </summary>
public sealed class CancelEmergencySosCommandValidator : AbstractValidator<CancelEmergencySosCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CancelEmergencySosCommandValidator"/> class.
    /// </summary>
    public CancelEmergencySosCommandValidator()
    {
        RuleFor(x => x.RequestId).NotEmpty();
        RuleFor(x => x.Reason).MaximumLength(512);
    }
}

/// <summary>
/// Handles <see cref="CancelEmergencySosCommand"/>.
/// </summary>
public sealed class CancelEmergencySosCommandHandler : IRequestHandler<CancelEmergencySosCommand, Result>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentCustomerService _currentCustomer;
    private readonly IDateTimeProvider _clock;
    private readonly IEmergencyDispatchService _dispatch;
    private readonly IEmergencyRealtimePublisher _realtime;
    private readonly IPaymentService _payments;

    /// <summary>
    /// Initializes a new instance of the <see cref="CancelEmergencySosCommandHandler"/> class.
    /// </summary>
    public CancelEmergencySosCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentCustomerService currentCustomer,
        IDateTimeProvider clock,
        IEmergencyDispatchService dispatch,
        IEmergencyRealtimePublisher realtime,
        IPaymentService payments)
    {
        _dbContext = dbContext;
        _currentCustomer = currentCustomer;
        _clock = clock;
        _dispatch = dispatch;
        _realtime = realtime;
        _payments = payments;
    }

    /// <inheritdoc />
    public async Task<Result> Handle(CancelEmergencySosCommand request, CancellationToken cancellationToken)
    {
        var customer = await _currentCustomer.GetOrCreateAsync(cancellationToken);
        var sos = await _dbContext.EmergencyRequests
            .Include(r => r.Category)
            .Include(r => r.Customer)
            .Include(r => r.Assignments).ThenInclude(a => a.Lawyer)
            .FirstOrDefaultAsync(r => r.Id == request.RequestId, cancellationToken)
            ?? throw new NotFoundException(nameof(EmergencyRequest), request.RequestId);

        if (sos.CustomerId != customer.Id)
        {
            throw new ForbiddenException("You are not allowed to cancel this SOS request.");
        }

        if (!EmergencyLifecycle.CanCancel(sos.Status))
        {
            throw new ConflictException("This SOS request is already closed.");
        }

        var now = _clock.UtcNow;
        var reason = string.IsNullOrWhiteSpace(request.Reason) ? "Cancelled by customer." : request.Reason.Trim();
        sos.Status = EmergencyStatus.Cancelled;
        sos.CancelledAtUtc = now;
        sos.CancellationReason = reason;
        sos.SearchExpiresAtUtc = null;

        foreach (var assignment in sos.Assignments.Where(a => a.Status == EmergencyAssignmentStatus.Pending
                                                              || a.Status == EmergencyAssignmentStatus.Accepted))
        {
            assignment.Status = EmergencyAssignmentStatus.Cancelled;
            assignment.RespondedAtUtc = now;
            assignment.CancellationReason = reason;
            await _realtime.PublishAssignmentCancelledAsync(
                assignment.Lawyer.UserId,
                sos.Id,
                assignment.Id,
                reason,
                cancellationToken);
        }

        await _payments.ReleaseEmergencyHoldAsync(
            sos.Id,
            reason,
            $"emergency-release:{sos.Id:D}",
            cancellationToken);

        _dispatch.AddTimeline(
            sos,
            EmergencyTimelineEventTypes.Cancelled,
            reason,
            customer.UserId,
            new { reason });
        await _dispatch.WriteHistoryAsync(sos, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _realtime.PublishStatusAsync(_dispatch.ToResponse(sos, sos.Category.Name), cancellationToken);
        return Result.Success();
    }
}
