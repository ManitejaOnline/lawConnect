namespace LawConnect.Application.Features.Lawyers.Models;

/// <summary>
/// Confirmation returned after a rating is submitted.
/// </summary>
public sealed record LawyerRatingSubmittedResponse(
    Guid RatingId,
    Guid LawyerId,
    int Stars,
    string? ReviewText,
    DateTime CreatedAt);

/// <summary>
/// Aggregate rating for a lawyer, including the Bayesian-shrunk score used by the Trust Score.
/// </summary>
public sealed record LawyerRatingSummaryResponse(
    double RatingAverage,
    int RatingCount,
    double BayesianScore);
