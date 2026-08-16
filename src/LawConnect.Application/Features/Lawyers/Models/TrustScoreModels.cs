namespace LawConnect.Application.Features.Lawyers.Models;

/// <summary>
/// Trust Score component breakdown. Points, not raw rates — already scaled to each component's max.
/// </summary>
public sealed record TrustScoreBreakdown(
    double ClientRating,
    double AppointmentReliability,
    double Responsiveness,
    double CaseUpdates,
    double CaseManagement,
    double VerifiedComplaints);

/// <summary>
/// Public Trust Score transparency response. Never includes complaint text/category — only the
/// numeric points those complaints contributed to the total.
/// </summary>
public sealed record LawyerTrustScoreResponse(
    int Score,
    string Level,
    double Rating,
    int ReviewCount,
    TrustScoreBreakdown Breakdown,
    string? CaseManagementNote);
