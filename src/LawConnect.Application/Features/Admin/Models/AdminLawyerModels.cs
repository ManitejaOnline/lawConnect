namespace LawConnect.Application.Features.Admin.Models;

/// <summary>
/// Pending/reviewed lawyer application summary for admin queues.
/// </summary>
public sealed record AdminLawyerApplicationSummary(
    Guid LawyerId,
    Guid UserId,
    string FullName,
    string? Email,
    string PhoneNumber,
    string? StateBarCouncil,
    string? EnrollmentNumber,
    string VerificationStatus,
    DateTime? ApplicationSubmittedAtUtc,
    DateTime CreatedAtUtc,
    string? CityName,
    string? StateName,
    string? CountryName,
    int YearsOfExperience,
    IReadOnlyList<string> PracticeAreas,
    DateTime? VerifiedAtUtc,
    DateTime? ReviewedAtUtc,
    string? ReviewReason);

/// <summary>
/// Full lawyer application for admin review.
/// </summary>
public sealed record AdminLawyerApplicationDetail(
    Guid LawyerId,
    Guid UserId,
    string FirstName,
    string LastName,
    string? Email,
    string PhoneNumber,
    string? ProfileImageUrl,
    string? StateBarCouncil,
    string? EnrollmentNumber,
    string? Bio,
    int YearsOfExperience,
    decimal ConsultationFee,
    string? CityName,
    string? StateName,
    string? CountryName,
    string VerificationStatus,
    bool IsVerified,
    DateTime? ApplicationSubmittedAtUtc,
    DateTime? VerifiedAtUtc,
    string? ReviewReason,
    DateTime? ReviewedAtUtc,
    DateTime CreatedAtUtc,
    IReadOnlyList<string> PracticeAreas,
    IReadOnlyList<string> Languages,
    IReadOnlyList<string> Education,
    IReadOnlyList<AdminLawyerDocumentItem> Documents);

/// <summary>
/// Document metadata for admin review. Storage keys are never returned.
/// </summary>
public sealed record AdminLawyerDocumentItem(
    Guid DocumentId,
    string DocumentType,
    string FileName,
    string ContentType,
    long FileSizeBytes,
    string VerificationStatus,
    string DownloadPath);
