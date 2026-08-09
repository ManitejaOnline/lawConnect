using LawConnect.Application.Common.Interfaces;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Common.Time;
using LawConnect.Application.Features.Appointments.Models;
using LawConnect.Application.Features.Lawyers.Services;
using LawConnect.Domain.Entities;
using LawConnect.Domain.Enums;
using LawConnect.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawConnect.Application.Features.Lawyers.Queries.GetLawyerAvailableSlots;

/// <summary>
/// Handles <see cref="GetLawyerAvailableSlotsQuery"/>.
/// </summary>
public sealed class GetLawyerAvailableSlotsQueryHandler
    : IRequestHandler<GetLawyerAvailableSlotsQuery, Result<IReadOnlyList<AvailableSlotResponse>>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetLawyerAvailableSlotsQueryHandler"/> class.
    /// </summary>
    public GetLawyerAvailableSlotsQueryHandler(
        IApplicationDbContext dbContext,
        IDateTimeProvider dateTimeProvider)
    {
        _dbContext = dbContext;
        _dateTimeProvider = dateTimeProvider;
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<AvailableSlotResponse>>> Handle(
        GetLawyerAvailableSlotsQuery request,
        CancellationToken cancellationToken)
    {
        var lawyer = await _dbContext.Lawyers
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == request.LawyerId, cancellationToken)
            ?? throw new NotFoundException(nameof(Lawyer), request.LawyerId);

        if (!lawyer.IsAcceptingAppointments || !LawyerOperationalRules.IsApproved(lawyer))
        {
            return Result.Success<IReadOnlyList<AvailableSlotResponse>>([]);
        }

        var timeZone = TimeZoneResolver.Resolve(request.TimeZoneId);
        var day = (DayOfWeekFlag)(int)request.Date.DayOfWeek;

        var windows = await _dbContext.LawyerAvailabilities
            .AsNoTracking()
            .Where(a => a.LawyerId == request.LawyerId && a.IsAvailable && a.DayOfWeek == day)
            .OrderBy(a => a.StartTime)
            .ToListAsync(cancellationToken);

        if (windows.Count == 0)
        {
            return Result.Success<IReadOnlyList<AvailableSlotResponse>>([]);
        }

        var localDayStart = request.Date.ToDateTime(TimeOnly.MinValue);
        var localDayEnd = request.Date.ToDateTime(TimeOnly.MaxValue);
        var dayStartUtc = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(localDayStart, DateTimeKind.Unspecified), timeZone);
        var dayEndUtc = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(localDayEnd, DateTimeKind.Unspecified), timeZone);

        var booked = await _dbContext.Appointments
            .AsNoTracking()
            .Where(a =>
                a.LawyerId == request.LawyerId
                && a.Status != AppointmentStatus.Cancelled
                && a.StartUtc < dayEndUtc
                && a.EndUtc > dayStartUtc)
            .Select(a => new { a.StartUtc, a.EndUtc })
            .ToListAsync(cancellationToken);

        var slotLength = TimeSpan.FromMinutes(request.SlotMinutes);
        var now = _dateTimeProvider.UtcNow;
        var slots = new List<AvailableSlotResponse>();

        foreach (var window in windows)
        {
            var cursor = window.StartTime;
            while (cursor.Add(slotLength) <= window.EndTime)
            {
                var localStart = request.Date.ToDateTime(cursor);
                var localEnd = request.Date.ToDateTime(cursor.Add(slotLength));
                var startUtc = TimeZoneInfo.ConvertTimeToUtc(
                    DateTime.SpecifyKind(localStart, DateTimeKind.Unspecified),
                    timeZone);
                var endUtc = TimeZoneInfo.ConvertTimeToUtc(
                    DateTime.SpecifyKind(localEnd, DateTimeKind.Unspecified),
                    timeZone);

                cursor = cursor.Add(slotLength);

                if (startUtc <= now)
                {
                    continue;
                }

                var overlaps = booked.Any(b => b.StartUtc < endUtc && b.EndUtc > startUtc);
                if (overlaps)
                {
                    continue;
                }

                slots.Add(new AvailableSlotResponse(startUtc, endUtc, request.TimeZoneId));
            }
        }

        return Result.Success<IReadOnlyList<AvailableSlotResponse>>(slots);
    }
}
