using LawConnect.Application.Common.Interfaces;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Appointments.Models;
using LawConnect.Application.Features.Appointments.Services;
using LawConnect.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawConnect.Application.Features.Appointments.Queries.GetUpcomingAppointments;

/// <summary>
/// Handles <see cref="GetUpcomingAppointmentsQuery"/>.
/// </summary>
public sealed class GetUpcomingAppointmentsQueryHandler
    : IRequestHandler<GetUpcomingAppointmentsQuery, Result<PagedResult<AppointmentResponse>>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetUpcomingAppointmentsQueryHandler"/> class.
    /// </summary>
    public GetUpcomingAppointmentsQueryHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    /// <inheritdoc />
    public async Task<Result<PagedResult<AppointmentResponse>>> Handle(
        GetUpcomingAppointmentsQuery request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetRequiredUserId();
        var now = _dateTimeProvider.UtcNow;
        var pagination = new PaginationRequest { PageNumber = request.PageNumber, PageSize = request.PageSize };

        var query = _dbContext.Appointments
            .AsNoTracking()
            .Include(a => a.Customer).ThenInclude(c => c.User)
            .Include(a => a.Lawyer).ThenInclude(l => l.User)
            .Where(a =>
                a.Status == AppointmentStatus.Scheduled
                && a.StartUtc >= now
                && (a.Customer.UserId == userId || a.Lawyer.UserId == userId))
            .OrderBy(a => a.StartUtc);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip(pagination.Skip)
            .Take(pagination.PageSize)
            .ToListAsync(cancellationToken);

        var page = new PagedResult<AppointmentResponse>(
            items.Select(AppointmentMapper.ToResponse).ToList(),
            total,
            pagination.PageNumber,
            pagination.PageSize);

        return Result.Success(page);
    }
}
