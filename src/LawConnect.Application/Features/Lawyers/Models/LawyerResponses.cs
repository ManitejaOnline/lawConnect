namespace LawConnect.Application.Features.Lawyers.Models;

/// <summary>
/// Lawyer profile response.
/// </summary>
public sealed record LawyerProfileResponse(
    Guid LawyerId,
    Guid UserId,
    string FirstName,
    string LastName,
    string? ProfileImageUrl,
    string? BarCouncilNumber,
    string? Bio,
    int YearsOfExperience,
    decimal ConsultationFee,
    decimal? EmergencyFee,
    bool IsVerified,
    bool IsAvailableForEmergency,
    bool IsAcceptingAppointments,
    bool IsOnline,
    DateTime? LastOnlineAtUtc,
    string VerificationStatus,
    DateTime? ApplicationSubmittedAtUtc,
    string? StateBarCouncil,
    string? CityName,
    string? StateName,
    string? CountryName,
    string? ReviewReason,
    DateTime? ReviewedAtUtc,
    double RatingAverage,
    int RatingCount,
    IReadOnlyList<PracticeAreaResponse> PracticeAreas,
    IReadOnlyList<LawyerEducationResponse> Educations,
    IReadOnlyList<LawyerExperienceResponse> Experiences,
    IReadOnlyList<LawyerLanguageResponse> Languages,
    IReadOnlyList<LawyerAvailabilityResponse> Availabilities);

/// <summary>
/// Practice area link response.
/// </summary>
public sealed record PracticeAreaResponse(
    Guid SpecializationId,
    string Name,
    int YearsOfExperience,
    bool IsPrimary);

/// <summary>
/// Education response.
/// </summary>
public sealed record LawyerEducationResponse(
    Guid Id,
    string InstitutionName,
    string Degree,
    string? FieldOfStudy,
    int? StartYear,
    int? EndYear,
    string? Description);

/// <summary>
/// Experience response.
/// </summary>
public sealed record LawyerExperienceResponse(
    Guid Id,
    string OrganizationName,
    string Title,
    DateOnly? StartDate,
    DateOnly? EndDate,
    bool IsCurrent,
    string? Description);

/// <summary>
/// Language response.
/// </summary>
public sealed record LawyerLanguageResponse(
    Guid Id,
    string LanguageCode,
    string LanguageName,
    string? Proficiency);

/// <summary>
/// Availability slot response.
/// </summary>
public sealed record LawyerAvailabilityResponse(
    Guid Id,
    string DayOfWeek,
    TimeOnly StartTime,
    TimeOnly EndTime,
    bool IsAvailable,
    string TimeZoneId);

/// <summary>
/// Verification document response.
/// </summary>
public sealed record LawyerDocumentResponse(
    Guid Id,
    string DocumentType,
    string FileName,
    string ContentType,
    long FileSizeBytes,
    string VerificationStatus,
    string? DownloadPath)
{
    /// <summary>
    /// Maps a stored document to an authorized download DTO. Storage keys are never returned.
    /// </summary>
    public static LawyerDocumentResponse FromEntity(Domain.Entities.LawyerDocument document, string downloadPath) =>
        new(
            document.Id,
            document.DocumentType.ToString(),
            document.FileName,
            document.ContentType,
            document.FileSizeBytes,
            document.VerificationStatus.ToString(),
            downloadPath);
}

/// <summary>
/// Practice area input for replace operations.
/// </summary>
public sealed record PracticeAreaInput(Guid SpecializationId, int YearsOfExperience, bool IsPrimary);

/// <summary>
/// Availability slot input for replace operations.
/// </summary>
public sealed record AvailabilitySlotInput(
    LawConnect.Domain.Enums.DayOfWeekFlag DayOfWeek,
    TimeOnly StartTime,
    TimeOnly EndTime,
    bool IsAvailable,
    string TimeZoneId);
