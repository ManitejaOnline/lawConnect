using LawConnect.Application.Features.Cases.Models;
using LawConnect.Domain.Entities;
using LawConnect.Domain.Enums;

namespace LawConnect.Application.Features.Cases.Services;

/// <summary>
/// Pure, side-effect-free calculations over already-loaded case sub-records — no DB access, so it
/// can be unit tested directly (mirrors <c>TrustScoreCalculator</c>'s shape). Computes the
/// "upcoming action" surfaced prominently on the case detail screen and the objective
/// activity/inactivity signals from section 12 of the spec. Never renders a verdict like
/// "stalled" — only measurable facts.
/// </summary>
public static class CaseActivityCalculator
{
    /// <summary>The soonest of the next scheduled hearing or next pending/in-progress milestone due date.</summary>
    public static UpcomingActionResponse? ComputeUpcomingAction(
        IReadOnlyCollection<Hearing> hearings,
        IReadOnlyCollection<CaseMilestone> milestones,
        DateTime now)
    {
        var nextHearing = hearings
            .Where(h => h.Status == HearingStatus.Scheduled && h.HearingDate >= now)
            .OrderBy(h => h.HearingDate)
            .FirstOrDefault();

        var nextMilestone = milestones
            .Where(m => (m.Status == MilestoneStatus.Pending || m.Status == MilestoneStatus.InProgress) && m.DueDate.HasValue)
            .OrderBy(m => m.DueDate)
            .FirstOrDefault();

        if (nextHearing is null && nextMilestone is null)
        {
            return null;
        }

        if (nextHearing is not null && (nextMilestone is null || nextHearing.HearingDate <= nextMilestone.DueDate!.Value))
        {
            return new UpcomingActionResponse(
                "Hearing", nextHearing.HearingDate, nextHearing.HearingType ?? "Hearing", nextHearing.CourtName, nextHearing.Status.ToString());
        }

        return new UpcomingActionResponse(
            "Milestone", nextMilestone!.DueDate!.Value, nextMilestone.Title, null, nextMilestone.Status.ToString());
    }

    /// <summary>Objective activity/inactivity signals — see the type's own doc comment for what each field means.</summary>
    public static CaseActivitySignalsResponse ComputeActivitySignals(
        Case caseEntity,
        IReadOnlyCollection<CaseUpdate> updates,
        IReadOnlyCollection<Hearing> hearings,
        IReadOnlyCollection<CaseMilestone> milestones,
        Guid lawyerUserId,
        DateTime now)
    {
        DateTime? lastUpdateAt = updates.Count == 0 ? null : updates.Max(u => u.CreatedAt);

        var activityDates = new List<DateTime> { caseEntity.CreatedAt };
        if (lastUpdateAt.HasValue)
        {
            activityDates.Add(lastUpdateAt.Value);
        }

        activityDates.AddRange(hearings.Where(h => h.Status != HearingStatus.Scheduled).Select(h => h.UpdatedAt ?? h.CreatedAt));
        activityDates.AddRange(milestones.Where(m => m.CompletedDate.HasValue).Select(m => m.CompletedDate!.Value));
        var lastActivityAt = activityDates.Max();

        var overdueMilestoneCount = milestones.Count(m => m.Status == MilestoneStatus.Overdue);
        var missedHearingCount = hearings.Count(h => h.Status == HearingStatus.Adjourned);
        var pendingActionCount =
            milestones.Count(m => m.Status is MilestoneStatus.Pending or MilestoneStatus.InProgress)
            + hearings.Count(h => h.Status == HearingStatus.Scheduled);

        var lastMeaningfulLawyerActivity = ComputeLastMeaningfulLawyerActivity(caseEntity, updates, hearings, milestones, lawyerUserId);
        var daysSinceLawyerActivity = (int)(now - lastMeaningfulLawyerActivity).TotalDays;
        var stallingStatus = ComputeStallingStatus(caseEntity.Status, daysSinceLawyerActivity);

        return new CaseActivitySignalsResponse(
            lastUpdateAt.HasValue ? (int)(now - lastUpdateAt.Value).TotalDays : null,
            (int)(now - lastActivityAt).TotalDays,
            overdueMilestoneCount,
            missedHearingCount,
            pendingActionCount,
            lastMeaningfulLawyerActivity,
            daysSinceLawyerActivity,
            stallingStatus.ToString());
    }

    /// <summary>
    /// The latest timestamp of any lawyer-authored, substantive action on the case: a case update
    /// created by the lawyer (this includes status changes and lawyer document uploads, both of
    /// which are logged as <see cref="CaseUpdate"/> rows), any hearing action, or any milestone
    /// action (hearings/milestones are always lawyer-authored already, so no filter is needed
    /// there). Customer-authored updates — including customer document uploads — are deliberately
    /// excluded: a customer sending something does not reset the lawyer's own progress timer.
    /// </summary>
    private static DateTime ComputeLastMeaningfulLawyerActivity(
        Case caseEntity,
        IReadOnlyCollection<CaseUpdate> updates,
        IReadOnlyCollection<Hearing> hearings,
        IReadOnlyCollection<CaseMilestone> milestones,
        Guid lawyerUserId)
    {
        var dates = new List<DateTime> { caseEntity.CreatedAt };
        dates.AddRange(updates.Where(u => u.CreatedByUserId == lawyerUserId).Select(u => u.CreatedAt));
        dates.AddRange(hearings.Select(h => h.UpdatedAt ?? h.CreatedAt));
        dates.AddRange(milestones.Select(m => m.CompletedDate ?? m.UpdatedAt ?? m.CreatedAt));
        return dates.Max();
    }

    /// <summary>
    /// Convenience overload for callers (e.g. Trust Score aggregation) that only need the
    /// classification, not the full <see cref="CaseActivitySignalsResponse"/> — avoids a string
    /// round-trip through the DTO's <c>StallingStatus</c> field.
    /// </summary>
    public static CaseStallingStatus ComputeStallingStatus(
        Case caseEntity,
        IReadOnlyCollection<CaseUpdate> updates,
        IReadOnlyCollection<Hearing> hearings,
        IReadOnlyCollection<CaseMilestone> milestones,
        Guid lawyerUserId,
        DateTime now)
    {
        var lastMeaningfulLawyerActivity = ComputeLastMeaningfulLawyerActivity(caseEntity, updates, hearings, milestones, lawyerUserId);
        var days = (int)(now - lastMeaningfulLawyerActivity).TotalDays;
        return ComputeStallingStatus(caseEntity.Status, days);
    }

    /// <summary>
    /// Classifies days-without-meaningful-lawyer-activity into a stalling severity. A case that has
    /// concluded (won/lost/settled/closed/cancelled) is never "stalled" — closing a case correctly
    /// must never look like a penalty, regardless of how long it took to get there.
    /// </summary>
    public static CaseStallingStatus ComputeStallingStatus(CaseStatus status, int daysSinceLastMeaningfulLawyerActivity)
    {
        var isTerminal = status is CaseStatus.Won or CaseStatus.Lost or CaseStatus.Settled or CaseStatus.Closed or CaseStatus.Cancelled;
        if (isTerminal)
        {
            return CaseStallingStatus.None;
        }

        return daysSinceLastMeaningfulLawyerActivity switch
        {
            >= 30 => CaseStallingStatus.SevereStalling,
            >= 14 => CaseStallingStatus.Stalling,
            >= 7 => CaseStallingStatus.Warning,
            _ => CaseStallingStatus.None
        };
    }

    /// <summary>
    /// Aggregates a lawyer's per-case stalling statuses into a Trust Score deduction. One shared
    /// formula, used by both <c>TrustScoreService</c> and the admin case-activity summary so the
    /// two surfaces can never disagree about the number. Deduction only grows with genuinely
    /// stalled cases — a single well-managed long-running case contributes nothing.
    /// </summary>
    public static (int StallingCount, int SevereCount, double Deduction) ComputeLawyerStallingDeduction(
        IReadOnlyCollection<CaseStallingStatus> caseStallingStatuses)
    {
        var stallingCount = caseStallingStatuses.Count(s => s == CaseStallingStatus.Stalling);
        var severeCount = caseStallingStatuses.Count(s => s == CaseStallingStatus.SevereStalling);
        var deduction = (stallingCount * 2) + (severeCount * 5);
        return (stallingCount, severeCount, deduction);
    }
}
