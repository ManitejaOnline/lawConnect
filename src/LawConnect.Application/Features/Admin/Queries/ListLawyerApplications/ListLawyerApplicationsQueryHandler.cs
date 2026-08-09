using LawConnect.Application.Common.Interfaces;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Admin.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawConnect.Application.Features.Admin.Queries.ListLawyerApplications;

/// <summary>
/// Handles <see cref="ListLawyerApplicationsQuery"/>.
/// </summary>
public sealed class ListLawyerApplicationsQueryHandler
    : IRequestHandler<ListLawyerApplicationsQuery, Result<PagedResult<AdminLawyerApplicationSummary>>>
{
    private readonly IApplicationDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="ListLawyerApplicationsQueryHandler"/> class.
    /// </summary>
    public ListLawyerApplicationsQueryHandler(IApplicationDbContext dbContext) => _dbContext = dbContext;

    /// <inheritdoc />
    public async Task<Result<PagedResult<AdminLawyerApplicationSummary>>> Handle(
        ListLawyerApplicationsQuery request,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.Lawyers.AsNoTracking().Include(l => l.User)
            .Include(l => l.LawyerSpecializations)
            .ThenInclude(s => s.Specialization)
            .AsQueryable();

        if (request.Status.HasValue)
        {
            query = query.Where(l => l.VerificationStatus == request.Status.Value);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(l => l.ApplicationSubmittedAtUtc ?? l.CreatedAt)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

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
            l.LawyerSpecializations.Select(s => s.Specialization.Name).ToArray())).ToArray();

        return Result.Success(new PagedResult<AdminLawyerApplicationSummary>(
            mapped,
            total,
            request.PageNumber,
            request.PageSize));
    }
}
