namespace LawConnect.Application.Features.Emergency.Services;

/// <summary>
/// Ranked lawyer candidate for SOS matching.
/// </summary>
public sealed record RankedEmergencyLawyer(
    Guid LawyerId,
    Guid UserId,
    double DistanceKm,
    double Score,
    double RatingAverage,
    double AcceptanceRate,
    double AverageResponseSeconds,
    int YearsOfExperience);
