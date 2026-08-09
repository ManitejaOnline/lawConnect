using LawConnect.Application.Common.Interfaces;
using LawConnect.Application.Common.Time;
using LawConnect.Application.Features.Lawyers.Services;
using LawConnect.Domain.Entities;
using LawConnect.Domain.Enums;
using LawConnect.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace LawConnect.Application.Features.Appointments.Services;

/// <summary>
/// Validates appointment slots against weekly availability and booking conflicts.
/// </summary>
public interface IAppointmentSlotValidator
{
    /// <summary>
    /// Ensures the requested slot is bookable for the lawyer.
    /// </summary>
    Task ValidateAsync(
        Guid lawyerId,
        DateTime startUtc,
        DateTime endUtc,
        string timeZoneId,
        Guid? excludeAppointmentId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Default <see cref="IAppointmentSlotValidator"/> implementation.
/// </summary>
public sealed class AppointmentSlotValidator : IAppointmentSlotValidator
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="AppointmentSlotValidator"/> class.
    /// </summary>
    public AppointmentSlotValidator(IApplicationDbContext dbContext, IDateTimeProvider dateTimeProvider)
    {
        _dbContext = dbContext;
        _dateTimeProvider = dateTimeProvider;
    }

    /// <inheritdoc />
    public async Task ValidateAsync(
        Guid lawyerId,
        DateTime startUtc,
        DateTime endUtc,
        string timeZoneId,
        Guid? excludeAppointmentId,
        CancellationToken cancellationToken = default)
    {
        if (endUtc <= startUtc)
        {
            throw new BusinessRuleException("Appointment end must be after start.", "Appointment.InvalidRange");
        }

        var duration = endUtc - startUtc;
        if (duration < TimeSpan.FromMinutes(15) || duration > TimeSpan.FromHours(4))
        {
            throw new BusinessRuleException(
                "Appointment duration must be between 15 minutes and 4 hours.",
                "Appointment.InvalidDuration");
        }

        if (startUtc <= _dateTimeProvider.UtcNow)
        {
            throw new BusinessRuleException("Appointment must start in the future.", "Appointment.MustBeFuture");
        }

        var lawyer = await _dbContext.Lawyers
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == lawyerId, cancellationToken)
            ?? throw new NotFoundException(nameof(Lawyer), lawyerId);

        if (!lawyer.IsAcceptingAppointments || !LawyerOperationalRules.IsApproved(lawyer))
        {
            throw new BusinessRuleException(
                "Lawyer is not accepting appointments.",
                "Appointment.NotAccepting");
        }

        var timeZone = TimeZoneResolver.Resolve(timeZoneId);
        var localStart = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(startUtc, DateTimeKind.Utc), timeZone);
        var localEnd = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(endUtc, DateTimeKind.Utc), timeZone);

        if (localStart.Date != localEnd.Date)
        {
            throw new BusinessRuleException(
                "Appointments must start and end on the same local day.",
                "Appointment.CrossDay");
        }

        var day = (DayOfWeekFlag)(int)localStart.DayOfWeek;
        var startTime = TimeOnly.FromDateTime(localStart);
        var endTime = TimeOnly.FromDateTime(localEnd);

        var windows = await _dbContext.LawyerAvailabilities
            .AsNoTracking()
            .Where(a =>
                a.LawyerId == lawyerId
                && a.IsAvailable
                && a.DayOfWeek == day)
            .ToListAsync(cancellationToken);

        var fitsWindow = windows.Any(w =>
            string.Equals(w.TimeZoneId, timeZoneId, StringComparison.OrdinalIgnoreCase)
            && startTime >= w.StartTime
            && endTime <= w.EndTime);

        if (!fitsWindow)
        {
            // Allow matching by day/time when timezone strings differ but convert equivalently.
            fitsWindow = windows.Any(w => startTime >= w.StartTime && endTime <= w.EndTime);
        }

        if (!fitsWindow)
        {
            throw new BusinessRuleException(
                "Requested slot is outside the lawyer's availability.",
                "Appointment.OutsideAvailability");
        }

        var conflictQuery = _dbContext.Appointments
            .AsNoTracking()
            .Where(a =>
                a.LawyerId == lawyerId
                && a.Status != AppointmentStatus.Cancelled
                && a.StartUtc < endUtc
                && a.EndUtc > startUtc);

        if (excludeAppointmentId.HasValue)
        {
            conflictQuery = conflictQuery.Where(a => a.Id != excludeAppointmentId.Value);
        }

        var hasConflict = await conflictQuery.AnyAsync(cancellationToken);
        if (hasConflict)
        {
            throw new ConflictException("The requested slot conflicts with an existing appointment.");
        }
    }
}
