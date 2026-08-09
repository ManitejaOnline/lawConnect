using LawConnect.Application.Common.Interfaces;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Appointments.Models;
using LawConnect.Application.Features.Appointments.Services;
using LawConnect.Domain.Entities;
using LawConnect.Domain.Enums;
using LawConnect.Domain.Exceptions;
using LawConnect.Shared.Constants;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LawConnect.Application.Features.Appointments.Commands.CancelAppointment;

/// <summary>
/// Handles <see cref="CancelAppointmentCommand"/>.
/// </summary>
public sealed class CancelAppointmentCommandHandler
    : IRequestHandler<CancelAppointmentCommand, Result<AppointmentResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUserNotificationDispatcher _notificationDispatcher;
    private readonly ILogger<CancelAppointmentCommandHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CancelAppointmentCommandHandler"/> class.
    /// </summary>
    public CancelAppointmentCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider,
        IUserNotificationDispatcher notificationDispatcher,
        ILogger<CancelAppointmentCommandHandler> logger)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
        _notificationDispatcher = notificationDispatcher;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<AppointmentResponse>> Handle(
        CancelAppointmentCommand request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetRequiredUserId();
        var appointment = await _dbContext.Appointments
            .Include(a => a.Customer).ThenInclude(c => c.User)
            .Include(a => a.Lawyer).ThenInclude(l => l.User)
            .FirstOrDefaultAsync(a => a.Id == request.AppointmentId, cancellationToken)
            ?? throw new NotFoundException(nameof(Appointment), request.AppointmentId);

        var isParticipant = appointment.Customer.UserId == userId || appointment.Lawyer.UserId == userId;
        if (!isParticipant)
        {
            throw new ForbiddenException("You are not allowed to cancel this appointment.");
        }

        if (appointment.Status == AppointmentStatus.Cancelled)
        {
            throw new ConflictException("Appointment is already cancelled.");
        }

        if (appointment.Status == AppointmentStatus.Completed)
        {
            throw new BusinessRuleException("Completed appointments cannot be cancelled.", "Appointment.AlreadyCompleted");
        }

        appointment.Status = AppointmentStatus.Cancelled;
        appointment.CancellationReason = string.IsNullOrWhiteSpace(request.Reason) ? null : request.Reason.Trim();
        appointment.CancelledAtUtc = _dateTimeProvider.UtcNow;
        appointment.CancelledByUserId = userId;

        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Cancelled appointment {AppointmentId} by user {UserId}", appointment.Id, userId);

        var recipientIds = new[] { appointment.Customer.UserId, appointment.Lawyer.UserId }
            .Where(id => id != userId)
            .Distinct();

        foreach (var recipientId in recipientIds)
        {
            await _notificationDispatcher.NotifyUserAsync(
                recipientId,
                "Appointment cancelled",
                "An appointment was cancelled.",
                NotificationTypes.AppointmentCancelled,
                new Dictionary<string, string>
                {
                    ["appointmentId"] = appointment.Id.ToString("D")
                },
                cancellationToken);
        }

        return Result.Success(AppointmentMapper.ToResponse(appointment));
    }
}
