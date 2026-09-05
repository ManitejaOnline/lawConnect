using LawConnect.Application.Common.Interfaces;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Appointments.Models;
using LawConnect.Application.Features.Appointments.Services;
using LawConnect.Application.Features.Lawyers.Services;
using LawConnect.Domain.Entities;
using LawConnect.Domain.Enums;
using LawConnect.Domain.Exceptions;
using LawConnect.Shared.Constants;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LawConnect.Application.Features.Appointments.Commands.CompleteAppointment;

/// <summary>
/// Handles <see cref="CompleteAppointmentCommand"/>.
/// </summary>
public sealed class CompleteAppointmentCommandHandler
    : IRequestHandler<CompleteAppointmentCommand, Result<AppointmentResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ITrustScoreService _trustScoreService;
    private readonly IUserNotificationDispatcher _notificationDispatcher;
    private readonly ILogger<CompleteAppointmentCommandHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CompleteAppointmentCommandHandler"/> class.
    /// </summary>
    public CompleteAppointmentCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider,
        ITrustScoreService trustScoreService,
        IUserNotificationDispatcher notificationDispatcher,
        ILogger<CompleteAppointmentCommandHandler> logger)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
        _trustScoreService = trustScoreService;
        _notificationDispatcher = notificationDispatcher;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<AppointmentResponse>> Handle(
        CompleteAppointmentCommand request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetRequiredUserId();
        var appointment = await _dbContext.Appointments
            .Include(a => a.Customer).ThenInclude(c => c.User)
            .Include(a => a.Lawyer).ThenInclude(l => l.User)
            .FirstOrDefaultAsync(a => a.Id == request.AppointmentId, cancellationToken)
            ?? throw new NotFoundException(nameof(Appointment), request.AppointmentId);

        if (appointment.Lawyer.UserId != userId)
        {
            throw new ForbiddenException("Only the assigned lawyer can complete this appointment.");
        }

        if (appointment.Status != AppointmentStatus.Scheduled)
        {
            throw new BusinessRuleException(
                "Only a scheduled appointment can be marked completed.",
                "Appointment.InvalidStatusForComplete");
        }

        var now = _dateTimeProvider.UtcNow;
        if (appointment.StartUtc > now)
        {
            throw new BusinessRuleException(
                "An appointment cannot be completed before its scheduled start time.",
                "Appointment.NotYetStarted");
        }

        appointment.Status = AppointmentStatus.Completed;
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _trustScoreService.RecalculateAsync(appointment.LawyerId, cancellationToken);

        _logger.LogInformation(
            "Appointment {AppointmentId} marked completed by lawyer {UserId}",
            appointment.Id,
            userId);

        await _notificationDispatcher.NotifyUserAsync(
            appointment.Customer.UserId,
            "Appointment completed",
            "Your consultation was marked complete. You can rate this lawyer.",
            NotificationTypes.AppointmentConfirmed,
            new Dictionary<string, string>
            {
                ["appointmentId"] = appointment.Id.ToString("D")
            },
            cancellationToken);

        return Result.Success(AppointmentMapper.ToResponse(appointment));
    }
}
