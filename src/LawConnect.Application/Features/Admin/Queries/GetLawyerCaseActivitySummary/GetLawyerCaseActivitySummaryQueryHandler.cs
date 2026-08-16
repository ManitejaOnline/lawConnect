using LawConnect.Application.Common.Interfaces;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Admin.Models;
using LawConnect.Application.Features.Cases.Services;
using LawConnect.Domain.Entities;
using LawConnect.Domain.Enums;
using LawConnect.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawConnect.Application.Features.Admin.Queries.GetLawyerCaseActivitySummary;

/// <summary>
/// Handles <see cref="GetLawyerCaseActivitySummaryQuery"/>.
/// </summary>
public sealed class GetLawyerCaseActivitySummaryQueryHandler
    : IRequestHandler<GetLawyerCaseActivitySummaryQuery, Result<LawyerCaseActivitySummaryResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IDateTimeProvider _clock;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetLawyerCaseActivitySummaryQueryHandler"/> class.
    /// </summary>
    public GetLawyerCaseActivitySummaryQueryHandler(IApplicationDbContext dbContext, IDateTimeProvider clock)
    {
        _dbContext = dbContext;
        _clock = clock;
    }

    /// <inheritdoc />
    public async Task<Result<LawyerCaseActivitySummaryResponse>> Handle(
        GetLawyerCaseActivitySummaryQuery request,
        CancellationToken cancellationToken)
    {
        var lawyer = await _dbContext.Lawyers.FirstOrDefaultAsync(l => l.Id == request.LawyerId, cancellationToken)
            ?? throw new NotFoundException(nameof(Lawyer), request.LawyerId);

        var cases = await _dbContext.Cases.Where(c => c.LawyerId == request.LawyerId).ToListAsync(cancellationToken);
        var caseIds = cases.Select(c => c.Id).ToList();

        var updates = await _dbContext.CaseUpdates.Where(u => caseIds.Contains(u.CaseId)).ToListAsync(cancellationToken);
        var updateCount = updates.Count;
        var hearings = await _dbContext.Hearings.Where(h => caseIds.Contains(h.CaseId)).ToListAsync(cancellationToken);
        var milestones = await _dbContext.CaseMilestones.Where(m => caseIds.Contains(m.CaseId)).ToListAsync(cancellationToken);

        var now = _clock.UtcNow;
        var earliestStart = cases.Count == 0 ? now : cases.Min(c => c.StartDate);
        var weeksElapsed = Math.Max(1.0, (now - earliestStart).TotalDays / 7.0);
        var updatesPerWeek = updateCount / weeksElapsed;

        var consideredMilestones = milestones.Where(m => m.Status != MilestoneStatus.Cancelled).ToList();
        var milestoneCompletionRate = consideredMilestones.Count == 0
            ? 0
            : (double)consideredMilestones.Count(m => m.Status == MilestoneStatus.Completed) / consideredMilestones.Count;

        var occurredHearings = hearings.Where(h => h.Status is HearingStatus.Completed or HearingStatus.Adjourned).ToList();
        var hearingAttendanceRate = occurredHearings.Count == 0
            ? 0
            : (double)occurredHearings.Count(h => h.Status == HearingStatus.Completed) / occurredHearings.Count;

        var stallingStatuses = cases.Select(c => CaseActivityCalculator.ComputeStallingStatus(
            c,
            updates.Where(u => u.CaseId == c.Id).ToList(),
            hearings.Where(h => h.CaseId == c.Id).ToList(),
            milestones.Where(m => m.CaseId == c.Id).ToList(),
            lawyer.UserId,
            now)).ToList();
        var (stallingCount, severeCount, deduction) = CaseActivityCalculator.ComputeLawyerStallingDeduction(stallingStatuses);

        var response = new LawyerCaseActivitySummaryResponse(
            request.LawyerId,
            cases.Count,
            cases.Count(c => c.Status == CaseStatus.Active),
            Math.Round(updatesPerWeek, 2),
            Math.Round(milestoneCompletionRate, 2),
            Math.Round(hearingAttendanceRate, 2),
            milestones.Count(m => m.Status == MilestoneStatus.Overdue),
            hearings.Count(h => h.Status == HearingStatus.Adjourned),
            stallingCount,
            severeCount,
            deduction);

        return Result.Success(response);
    }
}
