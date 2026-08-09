using LawConnect.Application.Common.Interfaces;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Common.Services;
using LawConnect.Application.Features.Appointments.Models;
using LawConnect.Application.Features.Appointments.Services;
using LawConnect.Domain.Entities;
using LawConnect.Domain.Enums;
using LawConnect.Domain.Exceptions;
using LawConnect.Shared.Constants;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LawConnect.Application.Features.Appointments.Commands.BookAppointment;

/// <summary>
/// Handles <see cref="BookAppointmentCommand"/>.
/// </summary>
public sealed class BookAppointmentCommandHandler
    : IRequestHandler<BookAppointmentCommand, Result<AppointmentResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentCustomerService _currentCustomer;
    private readonly IAppointmentSlotValidator _slotValidator;
    private readonly IUserNotificationDispatcher _notificationDispatcher;
    private readonly ILogger<BookAppointmentCommandHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="BookAppointmentCommandHandler"/> class.
    /// </summary>
    public BookAppointmentCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentCustomerService currentCustomer,
        IAppointmentSlotValidator slotValidator,
        IUserNotificationDispatcher notificationDispatcher,
        ILogger<BookAppointmentCommandHandler> logger)
    {
        _dbContext = dbContext;
        _currentCustomer = currentCustomer;
        _slotValidator = slotValidator;
        _notificationDispatcher = notificationDispatcher;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<AppointmentResponse>> Handle(
        BookAppointmentCommand request,
        CancellationToken cancellationToken)
    {
        var customer = await _currentCustomer.GetOrCreateAsync(cancellationToken);
        var lawyer = await _dbContext.Lawyers
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == request.LawyerId, cancellationToken)
            ?? throw new NotFoundException(nameof(Lawyer), request.LawyerId);

        await _slotValidator.ValidateAsync(
            request.LawyerId,
            request.StartUtc,
            request.EndUtc,
            request.TimeZoneId.Trim(),
            null,
            cancellationToken);

        var appointment = new Appointment
        {
            CustomerId = customer.Id,
            LawyerId = lawyer.Id,
            StartUtc = DateTime.SpecifyKind(request.StartUtc, DateTimeKind.Utc),
            EndUtc = DateTime.SpecifyKind(request.EndUtc, DateTimeKind.Utc),
            Status = AppointmentStatus.Scheduled,
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
            ConsultationFeeSnapshot = lawyer.ConsultationFee,
            TimeZoneId = request.TimeZoneId.Trim()
        };

        _dbContext.Appointments.Add(appointment);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var loaded = await LoadAsync(appointment.Id, cancellationToken);
        _logger.LogInformation(
            "Booked appointment {AppointmentId} with lawyer {LawyerId} for customer {CustomerId}",
            appointment.Id,
            lawyer.Id,
            customer.Id);

        await _notificationDispatcher.NotifyUserAsync(
            lawyer.UserId,
            "New booking",
            "A customer booked an appointment with you.",
            NotificationTypes.LawyerAcceptedBooking,
            new Dictionary<string, string>
            {
                ["appointmentId"] = appointment.Id.ToString("D"),
                ["lawyerId"] = lawyer.Id.ToString("D")
            },
            cancellationToken);

        await _notificationDispatcher.NotifyUserAsync(
            customer.UserId,
            "Appointment confirmed",
            "Your appointment has been booked.",
            NotificationTypes.AppointmentConfirmed,
            new Dictionary<string, string>
            {
                ["appointmentId"] = appointment.Id.ToString("D")
            },
            cancellationToken);

        return Result.Success(AppointmentMapper.ToResponse(loaded));
    }

    private Task<Appointment> LoadAsync(Guid id, CancellationToken cancellationToken) =>
        _dbContext.Appointments
            .AsNoTracking()
            .Include(a => a.Customer).ThenInclude(c => c.User)
            .Include(a => a.Lawyer).ThenInclude(l => l.User)
            .FirstAsync(a => a.Id == id, cancellationToken);
}
