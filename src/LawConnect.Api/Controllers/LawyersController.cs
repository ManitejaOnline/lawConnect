using LawConnect.Api.Extensions;
using LawConnect.Application.Features.Lawyers.Commands.AddLawyerEducation;
using LawConnect.Application.Features.Lawyers.Commands.AddLawyerExperience;
using LawConnect.Application.Features.Lawyers.Commands.AddLawyerLanguage;
using LawConnect.Application.Features.Lawyers.Commands.DeleteLawyerDocument;
using LawConnect.Application.Features.Lawyers.Commands.DeleteLawyerEducation;
using LawConnect.Application.Features.Lawyers.Commands.DeleteLawyerExperience;
using LawConnect.Application.Features.Lawyers.Commands.DeleteLawyerLanguage;
using LawConnect.Application.Features.Lawyers.Commands.RegisterLawyer;
using LawConnect.Application.Features.Lawyers.Commands.ReopenLawyerApplication;
using LawConnect.Application.Features.Lawyers.Commands.SaveLawyerApplication;
using LawConnect.Application.Features.Lawyers.Commands.SetLawyerAvailability;
using LawConnect.Application.Features.Lawyers.Commands.SubmitLawyerApplication;
using LawConnect.Application.Features.Lawyers.Commands.SubmitLawyerComplaint;
using LawConnect.Application.Features.Lawyers.Commands.SubmitLawyerRating;
using LawConnect.Application.Features.Lawyers.Commands.SetPracticeAreas;
using LawConnect.Application.Features.Lawyers.Commands.UpdateEmergencyAvailability;
using LawConnect.Application.Features.Lawyers.Commands.UpdateLawyerEducation;
using LawConnect.Application.Features.Lawyers.Commands.UpdateLawyerExperience;
using LawConnect.Application.Features.Lawyers.Commands.UpdateLawyerFees;
using LawConnect.Application.Features.Lawyers.Commands.UpdateLawyerLanguage;
using LawConnect.Application.Features.Lawyers.Commands.UpdateLawyerProfile;
using LawConnect.Application.Features.Lawyers.Commands.UpdateOnlineStatus;
using LawConnect.Application.Features.Lawyers.Commands.UploadLawyerDocument;
using LawConnect.Application.Features.Lawyers.Models;
using LawConnect.Application.Features.Lawyers.Queries.GetLawyerAvailableSlots;
using LawConnect.Application.Features.Lawyers.Queries.GetLawyerContact;
using LawConnect.Application.Features.Lawyers.Queries.GetLawyerProfile;
using LawConnect.Application.Features.Lawyers.Queries.GetLawyerRating;
using LawConnect.Application.Features.Lawyers.Queries.GetLawyerTrustScore;
using LawConnect.Application.Features.Lawyers.Queries.GetMyLawyerAvailability;
using LawConnect.Application.Features.Lawyers.Queries.GetMyLawyerDocuments;
using LawConnect.Application.Features.Lawyers.Queries.DownloadLawyerDocument;
using LawConnect.Application.Features.Lawyers.Queries.GetMyLawyerProfile;
using LawConnect.Application.Features.Lawyers.Queries.ListSpecializations;
using LawConnect.Application.Features.Lawyers.Queries.SearchLawyers;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Appointments.Models;
using LawConnect.Domain.Enums;
using LawConnect.Shared.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LawConnect.Api.Controllers;

/// <summary>
/// Lawyer registration and profile management endpoints.
/// </summary>
[ApiController]
[Route("api/lawyers")]
public sealed class LawyersController : ControllerBase
{
    private readonly ISender _sender;

    /// <summary>
    /// Initializes a new instance of the <see cref="LawyersController"/> class.
    /// </summary>
    /// <param name="sender">MediatR sender.</param>
    public LawyersController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Lists active practice areas.
    /// </summary>
    [HttpGet("specializations")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyList<SpecializationOptionResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListSpecializations(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ListSpecializationsQuery(), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Searches discoverable lawyers with filters, sorting, and pagination.
    /// </summary>
    /// <param name="search">Free-text search.</param>
    /// <param name="specializationId">Practice area filter.</param>
    /// <param name="languageCode">Language code filter.</param>
    /// <param name="minRating">Minimum rating.</param>
    /// <param name="minFee">Minimum consultation fee.</param>
    /// <param name="maxFee">Maximum consultation fee.</param>
    /// <param name="minExperience">Minimum years of experience.</param>
    /// <param name="isOnline">Online status filter.</param>
    /// <param name="isAvailableForEmergency">Emergency availability filter.</param>
    /// <param name="isAcceptingAppointments">Accepting appointments filter.</param>
    /// <param name="availableOnDay">Day-of-week availability filter.</param>
    /// <param name="availableFrom">Availability window start.</param>
    /// <param name="availableTo">Availability window end.</param>
    /// <param name="originLatitude">Origin latitude for distance.</param>
    /// <param name="originLongitude">Origin longitude for distance.</param>
    /// <param name="maxDistanceKm">Maximum distance in kilometers.</param>
    /// <param name="sortBy">Sort field (rating, fee, experience, distance, relevance).</param>
    /// <param name="sortDescending">Sort direction.</param>
    /// <param name="pageNumber">Page number.</param>
    /// <param name="pageSize">Page size.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paged lawyer search results.</returns>
    [HttpGet("search")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PagedResult<LawyerSearchItemResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Search(
        [FromQuery] string? search,
        [FromQuery] Guid? specializationId,
        [FromQuery] string? languageCode,
        [FromQuery] double? minRating,
        [FromQuery] decimal? minFee,
        [FromQuery] decimal? maxFee,
        [FromQuery] int? minExperience,
        [FromQuery] bool? isOnline,
        [FromQuery] bool? isAvailableForEmergency,
        [FromQuery] bool? isAcceptingAppointments,
        [FromQuery] DayOfWeekFlag? availableOnDay,
        [FromQuery] TimeOnly? availableFrom,
        [FromQuery] TimeOnly? availableTo,
        [FromQuery] double? originLatitude,
        [FromQuery] double? originLongitude,
        [FromQuery] double? maxDistanceKm,
        [FromQuery] string? sortBy,
        [FromQuery] bool sortDescending = false,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(
            new SearchLawyersQuery(
                search,
                specializationId,
                languageCode,
                minRating,
                minFee,
                maxFee,
                minExperience,
                isOnline,
                isAvailableForEmergency,
                isAcceptingAppointments,
                availableOnDay,
                availableFrom,
                availableTo,
                originLatitude,
                originLongitude,
                maxDistanceKm,
                sortBy,
                sortDescending,
                pageNumber,
                pageSize),
            cancellationToken);

        return result.ToActionResult();
    }

    /// <summary>
    /// Returns bookable slots for a lawyer on a local date.
    /// </summary>
    [HttpGet("{lawyerId:guid}/slots")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyList<AvailableSlotResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAvailableSlots(
        Guid lawyerId,
        [FromQuery] DateOnly date,
        [FromQuery] string timeZoneId = "Asia/Kolkata",
        [FromQuery] int slotMinutes = 30,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(
            new GetLawyerAvailableSlotsQuery(lawyerId, date, timeZoneId, slotMinutes),
            cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Registers the current user as a lawyer.
    /// </summary>
    [HttpPost("register")]
    [Authorize]
    [ProducesResponseType(typeof(LawyerProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterLawyerApiRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new RegisterLawyerCommand(
                request.BarCouncilNumber,
                request.Bio,
                request.YearsOfExperience,
                request.ConsultationFee,
                request.EmergencyFee,
                request.SpecializationIds),
            cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Gets a public lawyer profile by id.
    /// </summary>
    [HttpGet("{lawyerId:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LawyerProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid lawyerId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetLawyerProfileQuery(lawyerId), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Returns a verified lawyer's phone number for the native dialer. Authenticated customers only.
    /// </summary>
    [HttpGet("{lawyerId:guid}/contact")]
    [Authorize(Policy = PolicyNames.CustomerOnly)]
    [ProducesResponseType(typeof(LawyerContactResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetContact(Guid lawyerId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetLawyerContactQuery(lawyerId), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Gets the current user's lawyer profile.
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(LawyerProfileResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMine(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetMyLawyerProfileQuery(), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Saves lawyer application draft fields.
    /// </summary>
    [HttpPut("me/application")]
    [Authorize]
    [ProducesResponseType(typeof(LawyerProfileResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> SaveApplication(
        [FromBody] SaveLawyerApplicationApiRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new SaveLawyerApplicationCommand(
                request.StateBarCouncil,
                request.BarCouncilNumber,
                request.SpecializationIds,
                request.YearsOfExperience,
                request.Languages,
                request.Education,
                request.ConsultationFee,
                request.CityName,
                request.StateName,
                request.CountryName,
                request.Bio,
                request.Availability),
            cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Submits the lawyer application for admin verification.
    /// </summary>
    [HttpPost("me/application/submit")]
    [Authorize]
    [ProducesResponseType(typeof(LawyerProfileResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> SubmitApplication(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new SubmitLawyerApplicationCommand(), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Reopens a rejected application for resubmission.
    /// </summary>
    [HttpPost("me/application/reopen")]
    [Authorize]
    [ProducesResponseType(typeof(LawyerProfileResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> ReopenApplication(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ReopenLawyerApplicationCommand(), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Updates the current lawyer profile.
    /// </summary>
    [HttpPut("me")]
    [Authorize]
    [ProducesResponseType(typeof(LawyerProfileResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateProfile(
        [FromBody] UpdateLawyerProfileApiRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new UpdateLawyerProfileCommand(
                request.Bio,
                request.BarCouncilNumber,
                request.YearsOfExperience,
                request.IsAcceptingAppointments),
            cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Updates lawyer fees.
    /// </summary>
    [HttpPut("me/fees")]
    [Authorize]
    [ProducesResponseType(typeof(LawyerProfileResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateFees(
        [FromBody] UpdateLawyerFeesApiRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new UpdateLawyerFeesCommand(request.ConsultationFee, request.EmergencyFee),
            cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Updates emergency availability.
    /// </summary>
    [HttpPut("me/emergency-availability")]
    [Authorize]
    [ProducesResponseType(typeof(LawyerProfileResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateEmergencyAvailability(
        [FromBody] UpdateEmergencyAvailabilityApiRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new UpdateEmergencyAvailabilityCommand(request.IsAvailableForEmergency, request.EmergencyFee),
            cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Updates online status.
    /// </summary>
    [HttpPut("me/online-status")]
    [Authorize]
    [ProducesResponseType(typeof(LawyerProfileResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateOnlineStatus(
        [FromBody] UpdateOnlineStatusApiRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new UpdateOnlineStatusCommand(request.IsOnline), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Replaces practice areas.
    /// </summary>
    [HttpPut("me/practice-areas")]
    [Authorize]
    [ProducesResponseType(typeof(LawyerProfileResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> SetPracticeAreas(
        [FromBody] SetPracticeAreasApiRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new SetPracticeAreasCommand(request.PracticeAreas),
            cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Adds education.
    /// </summary>
    [HttpPost("me/educations")]
    [Authorize]
    [ProducesResponseType(typeof(LawyerEducationResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> AddEducation(
        [FromBody] LawyerEducationApiRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new AddLawyerEducationCommand(
                request.InstitutionName,
                request.Degree,
                request.FieldOfStudy,
                request.StartYear,
                request.EndYear,
                request.Description),
            cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Updates education.
    /// </summary>
    [HttpPut("me/educations/{educationId:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(LawyerEducationResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateEducation(
        Guid educationId,
        [FromBody] LawyerEducationApiRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new UpdateLawyerEducationCommand(
                educationId,
                request.InstitutionName,
                request.Degree,
                request.FieldOfStudy,
                request.StartYear,
                request.EndYear,
                request.Description),
            cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Deletes education.
    /// </summary>
    [HttpDelete("me/educations/{educationId:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteEducation(Guid educationId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new DeleteLawyerEducationCommand(educationId), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Adds experience.
    /// </summary>
    [HttpPost("me/experiences")]
    [Authorize]
    [ProducesResponseType(typeof(LawyerExperienceResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> AddExperience(
        [FromBody] LawyerExperienceApiRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new AddLawyerExperienceCommand(
                request.OrganizationName,
                request.Title,
                request.StartDate,
                request.EndDate,
                request.IsCurrent,
                request.Description),
            cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Updates experience.
    /// </summary>
    [HttpPut("me/experiences/{experienceId:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(LawyerExperienceResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateExperience(
        Guid experienceId,
        [FromBody] LawyerExperienceApiRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new UpdateLawyerExperienceCommand(
                experienceId,
                request.OrganizationName,
                request.Title,
                request.StartDate,
                request.EndDate,
                request.IsCurrent,
                request.Description),
            cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Deletes experience.
    /// </summary>
    [HttpDelete("me/experiences/{experienceId:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteExperience(Guid experienceId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new DeleteLawyerExperienceCommand(experienceId), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Adds a language.
    /// </summary>
    [HttpPost("me/languages")]
    [Authorize]
    [ProducesResponseType(typeof(LawyerLanguageResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> AddLanguage(
        [FromBody] LawyerLanguageApiRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new AddLawyerLanguageCommand(request.LanguageCode, request.LanguageName, request.Proficiency),
            cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Updates a language.
    /// </summary>
    [HttpPut("me/languages/{languageId:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(LawyerLanguageResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateLanguage(
        Guid languageId,
        [FromBody] UpdateLawyerLanguageApiRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new UpdateLawyerLanguageCommand(languageId, request.LanguageName, request.Proficiency),
            cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Deletes a language.
    /// </summary>
    [HttpDelete("me/languages/{languageId:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteLanguage(Guid languageId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new DeleteLawyerLanguageCommand(languageId), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Lists verification documents.
    /// </summary>
    [HttpGet("me/documents")]
    [Authorize]
    [ProducesResponseType(typeof(IReadOnlyList<LawyerDocumentResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDocuments(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetMyLawyerDocumentsQuery(), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Uploads a verification document.
    /// </summary>
    [HttpPost("me/documents")]
    [Authorize]
    [ProducesResponseType(typeof(LawyerDocumentResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> UploadDocument(
        [FromForm] DocumentType documentType,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new ProblemDetails { Title = "A non-empty file is required." });
        }

        await using var stream = file.OpenReadStream();
        var result = await _sender.Send(
            new UploadLawyerDocumentCommand(documentType, stream, file.FileName, file.ContentType, file.Length),
            cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Downloads a verification document owned by the current lawyer.
    /// </summary>
    [HttpGet("me/documents/{documentId:guid}/file")]
    [Authorize]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadMyDocument(Guid documentId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new DownloadLawyerDocumentQuery(documentId), cancellationToken);
        if (result.IsFailure)
        {
            return result.ToActionResult();
        }

        return File(result.Value.Content, result.Value.ContentType, result.Value.FileName);
    }

    /// <summary>
    /// Deletes a verification document.
    /// </summary>
    [HttpDelete("me/documents/{documentId:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteDocument(Guid documentId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new DeleteLawyerDocumentCommand(documentId), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Gets availability slots.
    /// </summary>
    [HttpGet("me/availability")]
    [Authorize]
    [ProducesResponseType(typeof(IReadOnlyList<LawyerAvailabilityResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAvailability(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetMyLawyerAvailabilityQuery(), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Replaces availability slots.
    /// </summary>
    [HttpPut("me/availability")]
    [Authorize]
    [ProducesResponseType(typeof(IReadOnlyList<LawyerAvailabilityResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SetAvailability(
        [FromBody] SetAvailabilityApiRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new SetLawyerAvailabilityCommand(request.Slots), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Submits a 1-5 star rating (with optional review) for a completed engagement.
    /// </summary>
    [HttpPost("{lawyerId:guid}/ratings")]
    [Authorize]
    [ProducesResponseType(typeof(LawyerRatingSubmittedResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> SubmitRating(
        Guid lawyerId,
        [FromBody] SubmitLawyerRatingApiRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new SubmitLawyerRatingCommand(lawyerId, request.AppointmentId, request.Stars, request.ReviewText),
            cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Gets a lawyer's aggregate rating.
    /// </summary>
    [HttpGet("{lawyerId:guid}/rating")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LawyerRatingSummaryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRating(Guid lawyerId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetLawyerRatingQuery(lawyerId), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Gets a lawyer's public Trust Score breakdown. Platform-observed reliability only.
    /// </summary>
    [HttpGet("{lawyerId:guid}/trust-score")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LawyerTrustScoreResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTrustScore(Guid lawyerId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetLawyerTrustScoreQuery(lawyerId), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Submits a complaint about a lawyer. Always starts Pending — never affects Trust Score
    /// unless an admin later verifies it.
    /// </summary>
    [HttpPost("{lawyerId:guid}/complaints")]
    [Authorize]
    [ProducesResponseType(typeof(LawyerComplaintSubmittedResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> SubmitComplaint(
        Guid lawyerId,
        [FromBody] SubmitLawyerComplaintApiRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new SubmitLawyerComplaintCommand(lawyerId, request.AppointmentId, request.Category, request.Description),
            cancellationToken);
        return result.ToActionResult();
    }
}

/// <summary>Submit rating API request.</summary>
public sealed record SubmitLawyerRatingApiRequest(Guid AppointmentId, int Stars, string? ReviewText);

/// <summary>Submit complaint API request.</summary>
public sealed record SubmitLawyerComplaintApiRequest(Guid? AppointmentId, string Category, string Description);

/// <summary>Register lawyer API request.</summary>
public sealed record RegisterLawyerApiRequest(
    string? BarCouncilNumber,
    string? Bio,
    int YearsOfExperience,
    decimal ConsultationFee,
    decimal? EmergencyFee,
    IReadOnlyList<Guid> SpecializationIds);

/// <summary>Update lawyer profile API request.</summary>
public sealed record UpdateLawyerProfileApiRequest(
    string? Bio,
    string? BarCouncilNumber,
    int YearsOfExperience,
    bool IsAcceptingAppointments);

/// <summary>Update fees API request.</summary>
public sealed record UpdateLawyerFeesApiRequest(decimal ConsultationFee, decimal? EmergencyFee);

/// <summary>Emergency availability API request.</summary>
public sealed record UpdateEmergencyAvailabilityApiRequest(bool IsAvailableForEmergency, decimal? EmergencyFee);

/// <summary>Online status API request.</summary>
public sealed record UpdateOnlineStatusApiRequest(bool IsOnline);

/// <summary>Practice areas API request.</summary>
public sealed record SetPracticeAreasApiRequest(IReadOnlyList<PracticeAreaInput> PracticeAreas);

/// <summary>Education API request.</summary>
public sealed record LawyerEducationApiRequest(
    string InstitutionName,
    string Degree,
    string? FieldOfStudy,
    int? StartYear,
    int? EndYear,
    string? Description);

/// <summary>Experience API request.</summary>
public sealed record LawyerExperienceApiRequest(
    string OrganizationName,
    string Title,
    DateOnly? StartDate,
    DateOnly? EndDate,
    bool IsCurrent,
    string? Description);

/// <summary>Language API request.</summary>
public sealed record LawyerLanguageApiRequest(string LanguageCode, string LanguageName, string? Proficiency);

/// <summary>Update language API request.</summary>
public sealed record UpdateLawyerLanguageApiRequest(string LanguageName, string? Proficiency);

/// <summary>Availability API request.</summary>
public sealed record SetAvailabilityApiRequest(IReadOnlyList<AvailabilitySlotInput> Slots);

/// <summary>Lawyer application draft payload.</summary>
public sealed record SaveLawyerApplicationApiRequest(
    string? StateBarCouncil,
    string? BarCouncilNumber,
    IReadOnlyList<Guid>? SpecializationIds,
    int? YearsOfExperience,
    string? Languages,
    string? Education,
    decimal? ConsultationFee,
    string? CityName,
    string? StateName,
    string? CountryName,
    string? Bio,
    IReadOnlyList<AvailabilitySlotInput>? Availability);
