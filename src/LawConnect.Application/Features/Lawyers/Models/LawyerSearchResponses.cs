namespace LawConnect.Application.Features.Lawyers.Models;

/// <summary>
/// Projected lawyer card used by discovery search.
/// </summary>
public sealed record LawyerSearchItemResponse(
    Guid LawyerId,
    Guid UserId,
    string FirstName,
    string LastName,
    string? ProfileImageUrl,
    string? Bio,
    int YearsOfExperience,
    decimal ConsultationFee,
    decimal? EmergencyFee,
    bool IsVerified,
    bool IsOnline,
    bool IsAcceptingAppointments,
    bool IsAvailableForEmergency,
    double RatingAverage,
    int RatingCount,
    string VerificationStatus,
    IReadOnlyList<string> PracticeAreas,
    IReadOnlyList<string> Languages,
    double? Latitude,
    double? Longitude,
    double? DistanceKm);

/// <summary>
/// Sort fields supported by lawyer discovery.
/// </summary>
public static class LawyerSearchSortFields
{
    /// <summary>Sort by rating average.</summary>
    public const string Rating = "rating";

    /// <summary>Sort by consultation fee.</summary>
    public const string Fee = "fee";

    /// <summary>Sort by years of experience.</summary>
    public const string Experience = "experience";

    /// <summary>Sort by distance from origin.</summary>
    public const string Distance = "distance";

    /// <summary>Default relevance / created date.</summary>
    public const string Relevance = "relevance";
}
