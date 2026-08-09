using LawConnect.Application.Common.Interfaces;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Appointments.Models;
using LawConnect.Application.Features.Appointments.Services;
using LawConnect.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawConnect.Application.Features.Appointments.Queries.GetAppointmentCalendar;

/// <summary>
/// Handles <see cref="GetAppointmentCalendarQuery"/>.
/// </summary>
public sealed class GetAppointmentCalendarQueryHandler
    : IRequestHandler<GetAppointmentCalendarQuery, Result<IReadOnlyList<AppointmentResponse>>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetAppointmentCalendarQueryHandler"/> class.
    /// </summary>
    public GetAppointmentCalendarQueryHandler(IApplicationDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<AppointmentResponse>>> Handle(
        GetAppointmentCalendarQuery request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetRequiredUserId();
        var from = DateTime.SpecifyKind(request.FromUtc, DateTimeKind.Utc);
        var to = DateTime.SpecifyKind(request.ToUtc, DateTimeKind.Utc);

        var items = await _dbContext.Appointments
            .AsNoTracking()
            .Include(a => a.Customer).ThenInclude(c => c.User)
            .Include(a => a.Lawyer).ThenInclude(l => l.User)
            .Where(a =>
                (a.Customer.UserId == userId || a.Lawyer.UserId == userId)
                && a.Status != AppointmentStatus.Cancelled
                && a.StartUtc < to
                && a.EndUtc > from)
            .OrderBy(a => a.StartUtc)
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<AppointmentResponse>>(
            items.Select(AppointmentMapper.ToResponse).ToList());
    }
}
