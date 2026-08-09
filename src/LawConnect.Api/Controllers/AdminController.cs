using LawConnect.Api.Extensions;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Admin.Commands.ReviewLawyerApplication;
using LawConnect.Application.Features.Admin.Models;
using LawConnect.Application.Features.Admin.Queries.DownloadLawyerDocumentForAdmin;
using LawConnect.Application.Features.Admin.Queries.GetLawyerApplication;
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
    /// Lists lawyer verification applications.
    /// </summary>
    [HttpGet("lawyers/applications")]
    [ProducesResponseType(typeof(PagedResult<AdminLawyerApplicationSummary>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListApplications(
        [FromQuery] VerificationStatus? status,
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
}

/// <summary>Admin status payload.</summary>
public sealed record AdminStatusResponse(string Status, string App);

/// <summary>Admin reject / request-changes reason.</summary>
public sealed record AdminReviewReasonApiRequest(string Reason);
