using LawConnect.Application.Common.Interfaces;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Admin.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LawConnect.Application.Features.Admin.Queries.ListComplaints;

/// <summary>
/// Handles <see cref="ListComplaintsQuery"/>.
/// </summary>
public sealed class ListComplaintsQueryHandler
    : IRequestHandler<ListComplaintsQuery, Result<PagedResult<AdminComplaintSummary>>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ILogger<ListComplaintsQueryHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ListComplaintsQueryHandler"/> class.
    /// </summary>
    public ListComplaintsQueryHandler(IApplicationDbContext dbContext, ILogger<ListComplaintsQueryHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<PagedResult<AdminComplaintSummary>>> Handle(
        ListComplaintsQuery request,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.LawyerComplaints
            .AsNoTracking()
            .Include(c => c.Lawyer).ThenInclude(l => l.User)
            .Include(c => c.Client).ThenInclude(c => c.User)
            .AsQueryable();

        if (request.Status.HasValue)
        {
            query = query.Where(c => c.Status == request.Status.Value);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(c => new AdminComplaintSummary(
                c.Id,
                c.LawyerId,
                $"{c.Lawyer.User.FirstName} {c.Lawyer.User.LastName}".Trim(),
                c.ClientId,
                $"{c.Client.User.FirstName} {c.Client.User.LastName}".Trim(),
                c.AppointmentId,
                c.Category,
                c.Description,
                c.Status.ToString(),
                c.Severity == null ? null : c.Severity.ToString(),
                c.AdminNotes,
                c.ReviewedAtUtc,
                c.CreatedAt))
            .ToArrayAsync(cancellationToken);

        _logger.LogInformation(
            "Admin complaint query status={Status} totalCount={TotalCount}",
            request.Status?.ToString() ?? "(any)",
            total);

        return Result.Success(new PagedResult<AdminComplaintSummary>(items, total, request.PageNumber, request.PageSize));
    }
}
