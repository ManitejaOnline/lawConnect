using LawConnect.Application.Common.Interfaces;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Appointments.Models;
using LawConnect.Application.Features.Appointments.Services;
using LawConnect.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawConnect.Application.Features.Appointments.Queries.GetAppointmentHistory;

/// <summary>
/// Handles <see cref="GetAppointmentHistoryQuery"/>.
/// </summary>
public sealed class GetAppointmentHistoryQueryHandler
    : IRequestHandler<GetAppointmentHistoryQuery, Result<PagedResult<AppointmentResponse>>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetAppointmentHistoryQueryHandler"/> class.
    /// </summary>
    public GetAppointmentHistoryQueryHandler(
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
        GetAppointmentHistoryQuery request,
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
                (a.Customer.UserId == userId || a.Lawyer.UserId == userId)
                && (a.Status != AppointmentStatus.Scheduled || a.EndUtc < now))
            .OrderByDescending(a => a.StartUtc);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip(pagination.Skip)
            .Take(pagination.PageSize)
            .ToListAsync(cancellationToken);

        return Result.Success(new PagedResult<AppointmentResponse>(
            items.Select(a => AppointmentMapper.ToResponse(a)).ToList(),
            total,
            pagination.PageNumber,
            pagination.PageSize));
    }
}
