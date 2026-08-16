using LawConnect.Api.Extensions;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Admin.Commands.DismissComplaint;
using LawConnect.Application.Features.Admin.Commands.ReviewLawyerApplication;
using LawConnect.Application.Features.Admin.Commands.VerifyComplaint;
using LawConnect.Application.Features.Admin.Models;
using LawConnect.Application.Features.Admin.Queries.AdminGetCaseById;
using LawConnect.Application.Features.Admin.Queries.AdminListCases;
using LawConnect.Application.Features.Admin.Queries.DownloadLawyerDocumentForAdmin;
using LawConnect.Application.Features.Admin.Queries.GetAdminLawyerTrustScore;
using LawConnect.Application.Features.Admin.Queries.GetLawyerApplication;
using LawConnect.Application.Features.Admin.Queries.GetLawyerCaseActivitySummary;
using LawConnect.Application.Features.Admin.Queries.ListComplaints;
using LawConnect.Application.Features.Admin.Queries.ListLawyerApplications;
using LawConnect.Domain.Enums;
using LawConnect.Shared.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LawConnect.Api.Controllers;

/// <summary>
/// Administrative API surface, including lawyer verification.
/// </summary>
[ApiController]
[Authorize(Policy = PolicyNames.AdminOnly)]
[Route("api/admin")]
public sealed class AdminController : ControllerBase
{
    private readonly ISender _sender;

    /// <summary>
    /// Initializes a new instance of the <see cref="AdminController"/> class.
    /// </summary>
    /// <param name="sender">MediatR sender.</param>
    public AdminController(ISender sender) => _sender = sender;

    /// <summary>
    /// Confirms the caller has the Admin role.
    /// </summary>
    [HttpGet("status")]
    [ProducesResponseType(typeof(AdminStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IActionResult Status() => Ok(new AdminStatusResponse("ok", "LawConnect Admin"));

    /// <summary>
    /// Lists lawyer verification applications. Repeat <c>status</c> to match any of several statuses,
    /// e.g. <c>?status=Pending&amp;status=UnderReview</c> for everything awaiting an admin decision.
    /// </summary>
    [HttpGet("lawyers/applications")]
    [ProducesResponseType(typeof(PagedResult<AdminLawyerApplicationSummary>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListApplications(
        [FromQuery] VerificationStatus[]? status,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(
            new ListLawyerApplicationsQuery(status, pageNumber, pageSize),
            cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Gets one lawyer application with documents and professional details.
    /// </summary>
    [HttpGet("lawyers/{lawyerId:guid}/application")]
    [ProducesResponseType(typeof(AdminLawyerApplicationDetail), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetApplication(Guid lawyerId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetLawyerApplicationQuery(lawyerId), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Downloads a lawyer verification document. Customers cannot call this endpoint.
    /// </summary>
    [HttpGet("lawyers/{lawyerId:guid}/documents/{documentId:guid}/file")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadDocument(
        Guid lawyerId,
        Guid documentId,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new DownloadLawyerDocumentForAdminQuery(lawyerId, documentId),
            cancellationToken);
        if (result.IsFailure)
        {
            return result.ToActionResult();
        }

        return File(result.Value.Content, result.Value.ContentType, result.Value.FileName);
    }

    /// <summary>
    /// Approves a lawyer application.
    /// </summary>
    [HttpPost("lawyers/{lawyerId:guid}/approve")]
    [ProducesResponseType(typeof(AdminLawyerApplicationDetail), StatusCodes.Status200OK)]
    public async Task<IActionResult> Approve(Guid lawyerId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new ReviewLawyerApplicationCommand(lawyerId, LawyerReviewAction.Approve, null),
            cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Rejects a lawyer application with a reason.
    /// </summary>
    [HttpPost("lawyers/{lawyerId:guid}/reject")]
    [ProducesResponseType(typeof(AdminLawyerApplicationDetail), StatusCodes.Status200OK)]
    public async Task<IActionResult> Reject(
        Guid lawyerId,
        [FromBody] AdminReviewReasonApiRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new ReviewLawyerApplicationCommand(lawyerId, LawyerReviewAction.Reject, request.Reason),
            cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Requests changes so the lawyer can resubmit.
    /// </summary>
    [HttpPost("lawyers/{lawyerId:guid}/request-changes")]
    [ProducesResponseType(typeof(AdminLawyerApplicationDetail), StatusCodes.Status200OK)]
    public async Task<IActionResult> RequestChanges(
        Guid lawyerId,
        [FromBody] AdminReviewReasonApiRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new ReviewLawyerApplicationCommand(lawyerId, LawyerReviewAction.RequestChanges, request.Reason),
            cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Gets a lawyer's full Trust Score view: breakdown, recalculation history, and complaints with
    /// full detail (never exposed to the public lawyer-facing endpoint).
    /// </summary>
    [HttpGet("lawyers/{lawyerId:guid}/trust-score")]
    [ProducesResponseType(typeof(AdminLawyerTrustScoreResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetLawyerTrustScore(Guid lawyerId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetAdminLawyerTrustScoreQuery(lawyerId), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Lists lawyer complaints for review, optionally filtered by status.
    /// </summary>
    [HttpGet("complaints")]
    [ProducesResponseType(typeof(PagedResult<AdminComplaintSummary>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListComplaints(
        [FromQuery] ComplaintStatus? status,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new ListComplaintsQuery(status, pageNumber, pageSize), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Marks a complaint Verified — the only way a complaint can affect a lawyer's Trust Score.
    /// </summary>
    [HttpPost("complaints/{complaintId:guid}/verify")]
    [ProducesResponseType(typeof(AdminComplaintSummary), StatusCodes.Status200OK)]
    public async Task<IActionResult> VerifyComplaint(
        Guid complaintId,
        [FromBody] VerifyComplaintApiRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new VerifyComplaintCommand(complaintId, request.Severity, request.Notes),
            cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Marks a complaint Dismissed — never affects Trust Score.
    /// </summary>
    [HttpPost("complaints/{complaintId:guid}/dismiss")]
    [ProducesResponseType(typeof(AdminComplaintSummary), StatusCodes.Status200OK)]
    public async Task<IActionResult> DismissComplaint(
        Guid complaintId,
        [FromBody] DismissComplaintApiRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new DismissComplaintCommand(complaintId, request.Notes), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Searches cases platform-wide. <c>overdueOnly</c> matches cases with at least one overdue
    /// milestone; <c>inactiveOnly</c> matches cases with no update in the last 14 days (or none ever).
    /// </summary>
    [HttpGet("cases")]
    [ProducesResponseType(typeof(PagedResult<AdminCaseListItemResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListCases(
        [FromQuery] CaseStatus? status,
        [FromQuery] Guid? lawyerId,
        [FromQuery] Guid? customerId,
        [FromQuery] bool overdueOnly = false,
        [FromQuery] bool inactiveOnly = false,
        [FromQuery] bool stallingOnly = false,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(
            new AdminListCasesQuery(status, lawyerId, customerId, overdueOnly, inactiveOnly, stallingOnly, pageNumber, pageSize),
            cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Gets full case detail — timeline, hearings, milestones, and updates in one response.
    /// </summary>
    [HttpGet("cases/{caseId:guid}")]
    [ProducesResponseType(typeof(AdminCaseDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCase(Guid caseId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new AdminGetCaseByIdQuery(caseId), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Gets objective, aggregate case-management signals for one lawyer — the data provider that
    /// can later feed the Trust Score system.
    /// </summary>
    [HttpGet("lawyers/{lawyerId:guid}/case-activity")]
    [ProducesResponseType(typeof(LawyerCaseActivitySummaryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetLawyerCaseActivity(Guid lawyerId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetLawyerCaseActivitySummaryQuery(lawyerId), cancellationToken);
        return result.ToActionResult();
    }
}

/// <summary>Admin status payload.</summary>
public sealed record AdminStatusResponse(string Status, string App);

/// <summary>Admin reject / request-changes reason.</summary>
public sealed record AdminReviewReasonApiRequest(string Reason);

/// <summary>Admin verify-complaint API request.</summary>
public sealed record VerifyComplaintApiRequest(ComplaintSeverity Severity, string? Notes);

/// <summary>Admin dismiss-complaint API request.</summary>
public sealed record DismissComplaintApiRequest(string? Notes);
