using LawConnect.Api.Extensions;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Emergency.Commands.AcceptEmergencyAssignment;
using LawConnect.Application.Features.Emergency.Commands.CancelEmergencySos;
using LawConnect.Application.Features.Emergency.Commands.CompleteEmergencySos;
using LawConnect.Application.Features.Emergency.Commands.CreateEmergencySos;
using LawConnect.Application.Features.Emergency.Commands.DeclineEmergencyAssignment;
using LawConnect.Application.Features.Emergency.Models;
using LawConnect.Application.Features.Emergency.Queries;
using LawConnect.Shared.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LawConnect.Api.Controllers;

/// <summary>
/// Emergency SOS endpoints for customers and lawyers.
/// </summary>
[ApiController]
[Authorize]
[Route("api/emergency")]
public sealed class EmergencyController : ControllerBase
{
    private readonly ISender _sender;

    /// <summary>
    /// Initializes a new instance of the <see cref="EmergencyController"/> class.
    /// </summary>
    public EmergencyController(ISender sender) => _sender = sender;

    /// <summary>
    /// Lists emergency categories.
    /// </summary>
    [HttpGet("categories")]
    [ProducesResponseType(typeof(IReadOnlyList<EmergencyCategoryResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCategories(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetEmergencyCategoriesQuery(), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Creates a customer SOS request.
    /// </summary>
    [HttpPost("sos")]
    [Authorize(Policy = PolicyNames.CustomerOnly)]
    [ProducesResponseType(typeof(EmergencyRequestResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateSos(
        [FromBody] CreateSosApiRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new CreateEmergencySosCommand(request.CategoryId, request.Latitude, request.Longitude, request.Notes),
            cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Gets the current customer's open SOS, if any.
    /// </summary>
    [HttpGet("sos/active")]
    [Authorize(Policy = PolicyNames.CustomerOnly)]
    [ProducesResponseType(typeof(EmergencyRequestResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> GetActiveSos(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetActiveEmergencySosQuery(), cancellationToken);
        if (!result.IsSuccess)
        {
            return result.ToActionResult();
        }

        return result.Value is null ? NoContent() : Ok(result.Value);
    }

    /// <summary>
    /// Gets the current lawyer's assigned open SOS, if any.
    /// </summary>
    [HttpGet("lawyer/active")]
    [Authorize(Policy = PolicyNames.LawyerOnly)]
    [ProducesResponseType(typeof(EmergencyRequestResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> GetLawyerActive(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetLawyerActiveEmergencyQuery(), cancellationToken);
        if (!result.IsSuccess)
        {
            return result.ToActionResult();
        }

        return result.Value is null ? NoContent() : Ok(result.Value);
    }

    /// <summary>
    /// Gets an SOS request.
    /// </summary>
    [HttpGet("sos/{requestId:guid}")]
    [ProducesResponseType(typeof(EmergencyRequestResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSos(Guid requestId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetEmergencyRequestQuery(requestId), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Gets SOS timeline events.
    /// </summary>
    [HttpGet("sos/{requestId:guid}/timeline")]
    [ProducesResponseType(typeof(IReadOnlyList<EmergencyTimelineItemResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTimeline(Guid requestId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetEmergencyTimelineQuery(requestId), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Cancels an active SOS request.
    /// </summary>
    [HttpPost("sos/{requestId:guid}/cancel")]
    [Authorize(Policy = PolicyNames.CustomerOnly)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Cancel(
        Guid requestId,
        [FromBody] CancelSosApiRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new CancelEmergencySosCommand(requestId, request.Reason), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Completes an assigned SOS request.
    /// </summary>
    [HttpPost("sos/{requestId:guid}/complete")]
    [ProducesResponseType(typeof(EmergencyRequestResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Complete(Guid requestId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new CompleteEmergencySosCommand(requestId), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Lists incoming SOS assignments for the current lawyer.
    /// </summary>
    [HttpGet("incoming")]
    [Authorize(Policy = PolicyNames.LawyerOnly)]
    [ProducesResponseType(typeof(IReadOnlyList<IncomingEmergencySosResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Incoming(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetIncomingEmergencyAssignmentsQuery(), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Accepts an SOS assignment.
    /// </summary>
    [HttpPost("assignments/{assignmentId:guid}/accept")]
    [Authorize(Policy = PolicyNames.LawyerOnly)]
    [ProducesResponseType(typeof(EmergencyRequestResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Accept(Guid assignmentId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new AcceptEmergencyAssignmentCommand(assignmentId), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Declines an SOS assignment.
    /// </summary>
    [HttpPost("assignments/{assignmentId:guid}/decline")]
    [Authorize(Policy = PolicyNames.LawyerOnly)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Decline(Guid assignmentId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new DeclineEmergencyAssignmentCommand(assignmentId), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Lists SOS history for the current customer or lawyer.
    /// </summary>
    [HttpGet("history")]
    [Authorize]
    [ProducesResponseType(typeof(PagedResult<EmergencyHistoryItemResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> History(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new GetMyEmergencyHistoryQuery(pageNumber, pageSize), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Returns aggregate SOS metrics.
    /// </summary>
    [HttpGet("metrics")]
    [Authorize(Policy = PolicyNames.AdminOnly)]
    [ProducesResponseType(typeof(EmergencyMetricsResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Metrics(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetEmergencyMetricsQuery(), cancellationToken);
        return result.ToActionResult();
    }
}

/// <summary>Create SOS API request.</summary>
public sealed record CreateSosApiRequest(Guid CategoryId, double Latitude, double Longitude, string? Notes);

/// <summary>Cancel SOS API request.</summary>
public sealed record CancelSosApiRequest(string? Reason);
