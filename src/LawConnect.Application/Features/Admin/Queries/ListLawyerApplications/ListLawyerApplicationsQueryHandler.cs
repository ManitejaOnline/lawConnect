using LawConnect.Application.Common.Interfaces;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Admin.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LawConnect.Application.Features.Admin.Queries.ListLawyerApplications;

/// <summary>
/// Handles <see cref="ListLawyerApplicationsQuery"/>.
/// </summary>
public sealed class ListLawyerApplicationsQueryHandler
    : IRequestHandler<ListLawyerApplicationsQuery, Result<PagedResult<AdminLawyerApplicationSummary>>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ILogger<ListLawyerApplicationsQueryHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ListLawyerApplicationsQueryHandler"/> class.
    /// </summary>
    public ListLawyerApplicationsQueryHandler(
        IApplicationDbContext dbContext,
        ILogger<ListLawyerApplicationsQueryHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<PagedResult<AdminLawyerApplicationSummary>>> Handle(
        ListLawyerApplicationsQuery request,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.Lawyers.AsNoTracking().Include(l => l.User)
            .Include(l => l.LawyerSpecializations)
            .ThenInclude(s => s.Specialization)
            .AsQueryable();

        if (request.Statuses is { Count: > 0 } statuses)
        {
            query = query.Where(l => statuses.Contains(l.VerificationStatus));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(l => l.ApplicationSubmittedAtUtc ?? l.CreatedAt)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        _logger.LogInformation(
            "Admin lawyer query statuses={Statuses} totalCount={TotalCount} returnedIds={LawyerIds}",
            request.Statuses is null ? "(any)" : string.Join(",", request.Statuses),
            total,
            string.Join(",", items.Select(i => i.Id)));

        var mapped = items.Select(l => new AdminLawyerApplicationSummary(
            l.Id,
            l.UserId,
            $"{l.User.FirstName} {l.User.LastName}".Trim(),
            l.User.Email,
            l.User.PhoneNumber,
            l.StateBarCouncil,
            l.BarCouncilNumber,
            l.VerificationStatus.ToString(),
            l.ApplicationSubmittedAtUtc,
            l.CreatedAt,
            l.CityName,
            l.StateName,
            l.CountryName,
            l.YearsOfExperience,
            l.LawyerSpecializations.Select(s => s.Specialization.Name).ToArray(),
            l.VerifiedAtUtc,
            l.ReviewedAtUtc,
            l.ReviewReason)).ToArray();

        return Result.Success(new PagedResult<AdminLawyerApplicationSummary>(
            mapped,
            total,
            request.PageNumber,
            request.PageSize));
    }
}
