using LawConnect.Application.Common.Interfaces;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Appointments.Models;
using LawConnect.Application.Features.Appointments.Services;
using LawConnect.Domain.Entities;
using LawConnect.Domain.Enums;
using LawConnect.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LawConnect.Application.Features.Appointments.Commands.RescheduleAppointment;

/// <summary>
/// Handles <see cref="RescheduleAppointmentCommand"/>.
/// </summary>
public sealed class RescheduleAppointmentCommandHandler
    : IRequestHandler<RescheduleAppointmentCommand, Result<AppointmentResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IAppointmentSlotValidator _slotValidator;
    private readonly ILogger<RescheduleAppointmentCommandHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RescheduleAppointmentCommandHandler"/> class.
    /// </summary>
    public RescheduleAppointmentCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        IAppointmentSlotValidator slotValidator,
        ILogger<RescheduleAppointmentCommandHandler> logger)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _slotValidator = slotValidator;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<AppointmentResponse>> Handle(
        RescheduleAppointmentCommand request,
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
            throw new ForbiddenException("You are not allowed to reschedule this appointment.");
        }

        if (appointment.Status != AppointmentStatus.Scheduled)
        {
            throw new BusinessRuleException(
                "Only scheduled appointments can be rescheduled.",
                "Appointment.InvalidStatus");
        }

        await _slotValidator.ValidateAsync(
            appointment.LawyerId,
            request.StartUtc,
            request.EndUtc,
            request.TimeZoneId.Trim(),
            appointment.Id,
            cancellationToken);

        appointment.StartUtc = DateTime.SpecifyKind(request.StartUtc, DateTimeKind.Utc);
        appointment.EndUtc = DateTime.SpecifyKind(request.EndUtc, DateTimeKind.Utc);
        appointment.TimeZoneId = request.TimeZoneId.Trim();

        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Rescheduled appointment {AppointmentId}", appointment.Id);
        return Result.Success(AppointmentMapper.ToResponse(appointment));
    }
}
