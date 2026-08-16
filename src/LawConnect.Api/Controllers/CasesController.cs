using LawConnect.Api.Extensions;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Cases.Commands.ChangeCaseStatus;
using LawConnect.Application.Features.Cases.Commands.CompleteMilestone;
using LawConnect.Application.Features.Cases.Commands.CreateCase;
using LawConnect.Application.Features.Cases.Commands.CreateCaseUpdate;
using LawConnect.Application.Features.Cases.Commands.CreateHearing;
using LawConnect.Application.Features.Cases.Commands.CreateMilestone;
using LawConnect.Application.Features.Cases.Commands.DeleteCaseDocument;
using LawConnect.Application.Features.Cases.Commands.RecordHearingOutcome;
using LawConnect.Application.Features.Cases.Commands.SubmitCaseRating;
using LawConnect.Application.Features.Cases.Commands.UpdateCase;
using LawConnect.Application.Features.Cases.Commands.UpdateHearing;
using LawConnect.Application.Features.Cases.Commands.UpdateMilestone;
using LawConnect.Application.Features.Cases.Commands.UploadCaseDocument;
using LawConnect.Application.Features.Cases.Models;
using LawConnect.Application.Features.Cases.Queries.DownloadCaseDocument;
using LawConnect.Application.Features.Cases.Queries.GetCaseById;
using LawConnect.Application.Features.Cases.Queries.GetCaseTimeline;
using LawConnect.Application.Features.Cases.Queries.ListCaseDocuments;
using LawConnect.Application.Features.Cases.Queries.ListCases;
using LawConnect.Application.Features.Cases.Queries.ListCaseUpdates;
using LawConnect.Application.Features.Cases.Queries.ListHearings;
using LawConnect.Application.Features.Cases.Queries.ListMilestones;
using LawConnect.Domain.Enums;
using LawConnect.Shared.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LawConnect.Api.Controllers;

/// <summary>
/// Case management endpoints — cases, updates, hearings, milestones, documents, and the unified
/// timeline. Writes are lawyer-only; reads are available to either party on a case, scoped by
/// ownership in the handler.
/// </summary>
[ApiController]
[Authorize]
[Route("api/cases")]
public sealed class CasesController : ControllerBase
{
    private readonly ISender _sender;

    /// <summary>
    /// Initializes a new instance of the <see cref="CasesController"/> class.
    /// </summary>
    public CasesController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>Creates a case for a customer the current lawyer has an appointment relationship with.</summary>
    [HttpPost]
    [Authorize(Policy = PolicyNames.LawyerOnly)]
    [ProducesResponseType(typeof(CaseResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Create([FromBody] CreateCaseApiRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new CreateCaseCommand(
                request.CustomerId,
                request.SpecializationId,
                request.Title,
                request.CaseNumber,
                request.Description,
                request.CourtName,
                request.Priority,
                request.StartDate,
                request.SourceAppointmentId),
            cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>Lists cases scoped to the current user (lawyer or customer).</summary>
    [HttpGet]
    [Authorize(Policy = PolicyNames.CustomerOrLawyer)]
    [ProducesResponseType(typeof(PagedResult<CaseListItemResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] CaseStatus? status,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new ListCasesQuery(status, pageNumber, pageSize), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>Gets full case detail, including the upcoming action and activity signals.</summary>
    [HttpGet("{caseId:guid}")]
    [Authorize(Policy = PolicyNames.CustomerOrLawyer)]
    [ProducesResponseType(typeof(CaseDetailResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById(Guid caseId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetCaseByIdQuery(caseId), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>Updates a case's descriptive fields.</summary>
    [HttpPut("{caseId:guid}")]
    [Authorize(Policy = PolicyNames.LawyerOnly)]
    [ProducesResponseType(typeof(CaseResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(Guid caseId, [FromBody] UpdateCaseApiRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new UpdateCaseCommand(caseId, request.SpecializationId, request.Title, request.CaseNumber, request.Description, request.CourtName, request.Priority),
            cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>Changes a case's status.</summary>
    [HttpPost("{caseId:guid}/status")]
    [Authorize(Policy = PolicyNames.LawyerOnly)]
    [ProducesResponseType(typeof(CaseResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> ChangeStatus(Guid caseId, [FromBody] ChangeCaseStatusApiRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ChangeCaseStatusCommand(caseId, request.Status, request.Note), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>Gets the unified chronological timeline for a case.</summary>
    [HttpGet("{caseId:guid}/timeline")]
    [Authorize(Policy = PolicyNames.CustomerOrLawyer)]
    [ProducesResponseType(typeof(IReadOnlyList<CaseTimelineEntryResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTimeline(Guid caseId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetCaseTimelineQuery(caseId), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>Logs a case update.</summary>
    [HttpPost("{caseId:guid}/updates")]
    [Authorize(Policy = PolicyNames.LawyerOnly)]
    [ProducesResponseType(typeof(CaseUpdateResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateUpdate(Guid caseId, [FromBody] CreateCaseUpdateApiRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new CreateCaseUpdateCommand(caseId, request.UpdateType, request.Title, request.Description), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>Lists all updates logged on a case.</summary>
    [HttpGet("{caseId:guid}/updates")]
    [Authorize(Policy = PolicyNames.CustomerOrLawyer)]
    [ProducesResponseType(typeof(IReadOnlyList<CaseUpdateResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListUpdates(Guid caseId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ListCaseUpdatesQuery(caseId), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>Schedules a hearing on a case.</summary>
    [HttpPost("{caseId:guid}/hearings")]
    [Authorize(Policy = PolicyNames.LawyerOnly)]
    [ProducesResponseType(typeof(HearingResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateHearing(Guid caseId, [FromBody] CreateHearingApiRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new CreateHearingCommand(caseId, request.HearingDate, request.CourtName, request.HearingType, request.Purpose),
            cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>Edits a scheduled hearing's fields.</summary>
    [HttpPut("{caseId:guid}/hearings/{hearingId:guid}")]
    [Authorize(Policy = PolicyNames.LawyerOnly)]
    [ProducesResponseType(typeof(HearingResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateHearing(Guid caseId, Guid hearingId, [FromBody] UpdateHearingApiRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new Application.Features.Cases.Commands.UpdateHearing.UpdateHearingCommand(caseId, hearingId, request.HearingDate, request.CourtName, request.HearingType, request.Purpose),
            cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>Records the outcome of a hearing that has taken place, been adjourned, or been cancelled.</summary>
    [HttpPost("{caseId:guid}/hearings/{hearingId:guid}/outcome")]
    [Authorize(Policy = PolicyNames.LawyerOnly)]
    [ProducesResponseType(typeof(HearingResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> RecordHearingOutcome(Guid caseId, Guid hearingId, [FromBody] RecordHearingOutcomeApiRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new RecordHearingOutcomeCommand(caseId, hearingId, request.Status, request.Outcome, request.Notes, request.NextAction),
            cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>Lists all hearings on a case.</summary>
    [HttpGet("{caseId:guid}/hearings")]
    [Authorize(Policy = PolicyNames.CustomerOrLawyer)]
    [ProducesResponseType(typeof(IReadOnlyList<HearingResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListHearings(Guid caseId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ListHearingsQuery(caseId), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>Creates a milestone on a case.</summary>
    [HttpPost("{caseId:guid}/milestones")]
    [Authorize(Policy = PolicyNames.LawyerOnly)]
    [ProducesResponseType(typeof(CaseMilestoneResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateMilestone(Guid caseId, [FromBody] CreateMilestoneApiRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new CreateMilestoneCommand(caseId, request.Title, request.Description, request.DueDate), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>Edits a milestone (except marking it Completed — use the complete action).</summary>
    [HttpPut("{caseId:guid}/milestones/{milestoneId:guid}")]
    [Authorize(Policy = PolicyNames.LawyerOnly)]
    [ProducesResponseType(typeof(CaseMilestoneResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateMilestone(Guid caseId, Guid milestoneId, [FromBody] UpdateMilestoneApiRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new Application.Features.Cases.Commands.UpdateMilestone.UpdateMilestoneCommand(caseId, milestoneId, request.Title, request.Description, request.DueDate, request.Status),
            cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>Marks a milestone as completed.</summary>
    [HttpPost("{caseId:guid}/milestones/{milestoneId:guid}/complete")]
    [Authorize(Policy = PolicyNames.LawyerOnly)]
    [ProducesResponseType(typeof(CaseMilestoneResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> CompleteMilestone(Guid caseId, Guid milestoneId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new CompleteMilestoneCommand(caseId, milestoneId), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>Lists all milestones on a case.</summary>
    [HttpGet("{caseId:guid}/milestones")]
    [Authorize(Policy = PolicyNames.CustomerOrLawyer)]
    [ProducesResponseType(typeof(IReadOnlyList<CaseMilestoneResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListMilestones(Guid caseId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ListMilestonesQuery(caseId), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>Uploads a file attached to a case, optionally scoped to an update/hearing/milestone. Either the case's lawyer or customer may upload.</summary>
    [HttpPost("{caseId:guid}/documents")]
    [Authorize(Policy = PolicyNames.CustomerOrLawyer)]
    [ProducesResponseType(typeof(CaseDocumentResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> UploadDocument(
        Guid caseId,
        [FromForm] Guid? caseUpdateId,
        [FromForm] Guid? hearingId,
        [FromForm] Guid? milestoneId,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new ProblemDetails { Title = "A non-empty file is required." });
        }

        await using var stream = file.OpenReadStream();
        var result = await _sender.Send(
            new UploadCaseDocumentCommand(caseId, caseUpdateId, hearingId, milestoneId, stream, file.FileName, file.ContentType, file.Length),
            cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>Lists all files attached to a case.</summary>
    [HttpGet("{caseId:guid}/documents")]
    [Authorize(Policy = PolicyNames.CustomerOrLawyer)]
    [ProducesResponseType(typeof(IReadOnlyList<CaseDocumentResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListDocuments(Guid caseId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ListCaseDocumentsQuery(caseId), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>Downloads a file attached to a case.</summary>
    [HttpGet("{caseId:guid}/documents/{documentId:guid}/file")]
    [Authorize(Policy = PolicyNames.CustomerOrLawyer)]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadDocument(Guid caseId, Guid documentId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new DownloadCaseDocumentQuery(caseId, documentId), cancellationToken);
        if (result.IsFailure)
        {
            return result.ToActionResult();
        }

        return File(result.Value.Content, result.Value.ContentType, result.Value.FileName);
    }

    /// <summary>Deletes a file attached to a case. Permitted for the uploader or the case's lawyer.</summary>
    [HttpDelete("{caseId:guid}/documents/{documentId:guid}")]
    [Authorize(Policy = PolicyNames.CustomerOrLawyer)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteDocument(Guid caseId, Guid documentId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new DeleteCaseDocumentCommand(caseId, documentId), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>Submits a 1-5 star rating (with optional review) for a case that has concluded.</summary>
    [HttpPost("{caseId:guid}/rating")]
    [Authorize(Policy = PolicyNames.CustomerOnly)]
    [ProducesResponseType(typeof(CaseRatingResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> SubmitRating(Guid caseId, [FromBody] SubmitCaseRatingApiRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new SubmitCaseRatingCommand(caseId, request.Stars, request.ReviewText), cancellationToken);
        return result.ToActionResult();
    }
}

/// <summary>Request body for <see cref="CasesController.Create"/>.</summary>
public sealed record CreateCaseApiRequest(
    Guid CustomerId,
    Guid SpecializationId,
    string Title,
    string? CaseNumber,
    string? Description,
    string? CourtName,
    CasePriority Priority,
    DateTime StartDate,
    Guid? SourceAppointmentId);

/// <summary>Request body for <see cref="CasesController.Update"/>.</summary>
public sealed record UpdateCaseApiRequest(
    Guid SpecializationId,
    string Title,
    string? CaseNumber,
    string? Description,
    string? CourtName,
    CasePriority Priority);

/// <summary>Request body for <see cref="CasesController.ChangeStatus"/>.</summary>
public sealed record ChangeCaseStatusApiRequest(CaseStatus Status, string? Note);

/// <summary>Request body for <see cref="CasesController.CreateUpdate"/>.</summary>
public sealed record CreateCaseUpdateApiRequest(CaseUpdateType UpdateType, string Title, string Description);

/// <summary>Request body for <see cref="CasesController.CreateHearing"/>.</summary>
public sealed record CreateHearingApiRequest(DateTime HearingDate, string? CourtName, string? HearingType, string? Purpose);

/// <summary>Request body for <see cref="CasesController.UpdateHearing"/>.</summary>
public sealed record UpdateHearingApiRequest(DateTime HearingDate, string? CourtName, string? HearingType, string? Purpose);

/// <summary>Request body for <see cref="CasesController.RecordHearingOutcome"/>.</summary>
public sealed record RecordHearingOutcomeApiRequest(HearingStatus Status, string? Outcome, string? Notes, string? NextAction);

/// <summary>Request body for <see cref="CasesController.CreateMilestone"/>.</summary>
public sealed record CreateMilestoneApiRequest(string Title, string? Description, DateTime? DueDate);

/// <summary>Request body for <see cref="CasesController.UpdateMilestone"/>.</summary>
public sealed record UpdateMilestoneApiRequest(string Title, string? Description, DateTime? DueDate, MilestoneStatus Status);

/// <summary>Request body for <see cref="CasesController.SubmitRating"/>.</summary>
public sealed record SubmitCaseRatingApiRequest(int Stars, string? ReviewText);
