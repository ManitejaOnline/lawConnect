using LawConnect.Application.Common.Interfaces;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Admin.Models;
using LawConnect.Application.Features.Cases.Services;
using LawConnect.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawConnect.Application.Features.Admin.Queries.AdminListCases;

/// <summary>
/// Handles <see cref="AdminListCasesQuery"/>.
/// </summary>
public sealed class AdminListCasesQueryHandler : IRequestHandler<AdminListCasesQuery, Result<PagedResult<AdminCaseListItemResponse>>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IDateTimeProvider _clock;

    /// <summary>
    /// Initializes a new instance of the <see cref="AdminListCasesQueryHandler"/> class.
    /// </summary>
    public AdminListCasesQueryHandler(IApplicationDbContext dbContext, IDateTimeProvider clock)
    {
        _dbContext = dbContext;
        _clock = clock;
    }

    /// <inheritdoc />
    public async Task<Result<PagedResult<AdminCaseListItemResponse>>> Handle(AdminListCasesQuery request, CancellationToken cancellationToken)
    {
        var query = _dbContext.Cases
            .Include(c => c.Customer).ThenInclude(c => c.User)
            .Include(c => c.Lawyer).ThenInclude(l => l.User)
            .Include(c => c.Specialization)
            .AsQueryable();

        if (request.Status is { } status)
        {
            query = query.Where(c => c.Status == status);
        }

        if (request.LawyerId is { } lawyerId)
        {
            query = query.Where(c => c.LawyerId == lawyerId);
        }

        if (request.CustomerId is { } customerId)
        {
            query = query.Where(c => c.CustomerId == customerId);
        }

        var candidates = await query.OrderByDescending(c => c.UpdatedAt ?? c.CreatedAt).ToListAsync(cancellationToken);
        var caseIds = candidates.Select(c => c.Id).ToList();

        var hearings = await _dbContext.Hearings.Where(h => caseIds.Contains(h.CaseId)).ToListAsync(cancellationToken);
        var milestones = await _dbContext.CaseMilestones.Where(m => caseIds.Contains(m.CaseId)).ToListAsync(cancellationToken);
        var allUpdates = await _dbContext.CaseUpdates.Where(u => caseIds.Contains(u.CaseId)).ToListAsync(cancellationToken);
        var lastUpdateByCase = allUpdates
            .GroupBy(u => u.CaseId)
            .Select(g => new { CaseId = g.Key, Last = g.Max(u => u.CreatedAt) })
            .ToList();

        var now = _clock.UtcNow;
        const int inactivityThresholdDays = 14;

        var items = candidates.Select(c =>
        {
            var caseHearings = hearings.Where(h => h.CaseId == c.Id).ToList();
            var caseMilestones = milestones.Where(m => m.CaseId == c.Id).ToList();
            var caseUpdates = allUpdates.Where(u => u.CaseId == c.Id).ToList();
            var overdueCount = caseMilestones.Count(m => m.Status == MilestoneStatus.Overdue);
            var upcoming = CaseActivityCalculator.ComputeUpcomingAction(caseHearings, caseMilestones, now);
            var lastUpdateAt = lastUpdateByCase.FirstOrDefault(x => x.CaseId == c.Id)?.Last;
            var daysSinceLastUpdate = lastUpdateAt.HasValue ? (int)(now - lastUpdateAt.Value).TotalDays : (int?)null;
            var signals = CaseActivityCalculator.ComputeActivitySignals(c, caseUpdates, caseHearings, caseMilestones, c.Lawyer.UserId, now);

            return new AdminCaseListItemResponse(
                c.Id,
                c.Title,
                c.CaseNumber,
                $"{c.Lawyer.User.FirstName} {c.Lawyer.User.LastName}".Trim(),
                $"{c.Customer.User.FirstName} {c.Customer.User.LastName}".Trim(),
                c.Specialization.Name,
                c.Status.ToString(),
                c.Priority.ToString(),
                c.StartDate,
                upcoming,
                daysSinceLastUpdate,
                overdueCount,
                signals.DaysSinceLastMeaningfulLawyerActivity,
                signals.StallingStatus);
        });

        if (request.OverdueOnly)
        {
            items = items.Where(i => i.OverdueMilestoneCount > 0);
        }

        if (request.InactiveOnly)
        {
            items = items.Where(i => i.DaysSinceLastUpdate is null || i.DaysSinceLastUpdate >= inactivityThresholdDays);
        }

        if (request.StallingOnly)
        {
            items = items.Where(i => i.StallingStatus is nameof(CaseStallingStatus.Stalling) or nameof(CaseStallingStatus.SevereStalling));
        }

        var filtered = items.ToList();
        var totalCount = filtered.Count;
        var page = filtered.Skip((request.PageNumber - 1) * request.PageSize).Take(request.PageSize).ToList();

        return Result.Success(new PagedResult<AdminCaseListItemResponse>(page, totalCount, request.PageNumber, request.PageSize));
    }
}
