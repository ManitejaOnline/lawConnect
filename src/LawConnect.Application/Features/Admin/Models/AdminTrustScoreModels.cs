using LawConnect.Application.Features.Lawyers.Models;

namespace LawConnect.Application.Features.Admin.Models;

/// <summary>
/// One historical Trust Score recalculation snapshot.
/// </summary>
public sealed record TrustScoreHistoryEntry(
    int Score,
    string Level,
    TrustScoreBreakdown Breakdown,
    string? CaseManagementNote,
    DateTime ComputedAtUtc);

/// <summary>
/// Admin's full view of a lawyer's Trust Score: current breakdown, recalculation history, and
/// linked complaints with full detail (never exposed to the public endpoint).
/// </summary>
public sealed record AdminLawyerTrustScoreResponse(
    LawyerTrustScoreResponse Current,
    IReadOnlyList<TrustScoreHistoryEntry> History,
    IReadOnlyList<AdminComplaintSummary> Complaints);
