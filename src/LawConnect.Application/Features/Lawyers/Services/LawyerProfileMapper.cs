using LawConnect.Application.Features.Lawyers.Models;
using LawConnect.Domain.Entities;

namespace LawConnect.Application.Features.Lawyers.Services;

/// <summary>
/// Maps lawyer aggregate graphs to response DTOs.
/// </summary>
public static class LawyerProfileMapper
{
    /// <summary>
    /// Maps a loaded lawyer aggregate to <see cref="LawyerProfileResponse"/>.
    /// </summary>
    public static LawyerProfileResponse ToProfileResponse(Lawyer lawyer) =>
        new(
            lawyer.Id,
            lawyer.UserId,
            lawyer.User.FirstName,
            lawyer.User.LastName,
            lawyer.User.ProfileImageUrl,
            lawyer.BarCouncilNumber,
            lawyer.Bio,
            lawyer.YearsOfExperience,
            lawyer.ConsultationFee,
            lawyer.EmergencyFee,
            lawyer.IsVerified,
            lawyer.IsAvailableForEmergency,
            lawyer.IsAcceptingAppointments,
            lawyer.IsOnline,
            lawyer.LastOnlineAtUtc,
            lawyer.VerificationStatus.ToString(),
            lawyer.ApplicationSubmittedAtUtc,
            lawyer.StateBarCouncil,
            lawyer.CityName,
            lawyer.StateName,
            lawyer.CountryName,
            lawyer.ReviewReason,
            lawyer.ReviewedAtUtc,
            lawyer.RatingAverage,
            lawyer.RatingCount,
            lawyer.LawyerSpecializations
                .OrderByDescending(x => x.IsPrimary)
                .Select(x => new PracticeAreaResponse(
                    x.SpecializationId,
                    x.Specialization.Name,
                    x.YearsOfExperience,
                    x.IsPrimary))
                .ToArray(),
            lawyer.Educations
                .OrderByDescending(x => x.EndYear ?? x.StartYear ?? 0)
                .Select(x => new LawyerEducationResponse(
                    x.Id,
                    x.InstitutionName,
                    x.Degree,
                    x.FieldOfStudy,
                    x.StartYear,
                    x.EndYear,
                    x.Description))
                .ToArray(),
            lawyer.Experiences
                .OrderByDescending(x => x.IsCurrent)
                .ThenByDescending(x => x.EndDate ?? x.StartDate)
                .Select(x => new LawyerExperienceResponse(
                    x.Id,
                    x.OrganizationName,
                    x.Title,
                    x.StartDate,
                    x.EndDate,
                    x.IsCurrent,
                    x.Description))
                .ToArray(),
            lawyer.Languages
                .OrderBy(x => x.LanguageName)
                .Select(x => new LawyerLanguageResponse(x.Id, x.LanguageCode, x.LanguageName, x.Proficiency))
                .ToArray(),
            lawyer.Availabilities
                .OrderBy(x => x.DayOfWeek)
                .ThenBy(x => x.StartTime)
                .Select(x => new LawyerAvailabilityResponse(
                    x.Id,
                    x.DayOfWeek.ToString(),
                    x.StartTime,
                    x.EndTime,
                    x.IsAvailable,
                    x.TimeZoneId))
                .ToArray());
}
