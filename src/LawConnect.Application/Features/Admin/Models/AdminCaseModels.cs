using LawConnect.Application.Features.Cases.Models;

namespace LawConnect.Application.Features.Admin.Models;

/// <summary>
/// Lightweight case DTO for the admin case list.
/// </summary>
public sealed record AdminCaseListItemResponse(
    Guid Id,
    string Title,
    string? CaseNumber,
    string LawyerName,
    string CustomerName,
    string CaseType,
    string Status,
    string Priority,
    DateTime StartDate,
    UpcomingActionResponse? UpcomingAction,
    int? DaysSinceLastUpdate,
    int OverdueMilestoneCount,
    int? DaysSinceLastMeaningfulLawyerActivity,
    string StallingStatus);

/// <summary>
/// Full case detail for admin — the case, computed signals, and the complete timeline/hearings/
/// milestones/updates in one response so the admin UI does not need N round trips.
/// </summary>
public sealed record AdminCaseDetailResponse(
    CaseResponse Case,
    UpcomingActionResponse? UpcomingAction,
    CaseActivitySignalsResponse ActivitySignals,
    IReadOnlyList<CaseTimelineEntryResponse> Timeline,
    IReadOnlyList<HearingResponse> Hearings,
    IReadOnlyList<CaseMilestoneResponse> Milestones,
    IReadOnlyList<CaseUpdateResponse> Updates);

/// <summary>
/// Objective, aggregate case-management signals for one lawyer across all their cases. A read-only
/// data provider — see the type's home for why it is not currently wired into the Trust Score.
/// </summary>
public sealed record LawyerCaseActivitySummaryResponse(
    Guid LawyerId,
    int TotalCases,
    int ActiveCases,
    double UpdatesPerWeek,
    double MilestoneCompletionRate,
    double HearingAttendanceRate,
    int OverdueMilestoneCount,
    int MissedHearingCount,
    int StallingCaseCount,
    int SeverelyStallingCaseCount,
    double TrustScoreDeduction);
