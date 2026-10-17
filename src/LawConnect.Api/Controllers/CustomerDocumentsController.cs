#pragma warning disable CS1591
using LawConnect.Application.Common.Interfaces;
using LawConnect.Application.Features.Documents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Net.Http.Headers;

namespace LawConnect.Api.Controllers;

/// <summary>
/// Metadata and live relay for documents that stay on the customer's phone.
/// </summary>
[ApiController]
[Authorize]
[Route("api/customer-documents")]
public sealed class CustomerDocumentsController : ControllerBase
{
    private readonly IDocumentAccess _access;
    private readonly IDocumentTransferRelay _relay;
    private readonly ICurrentUser _currentUser;

    public CustomerDocumentsController(IDocumentAccess access, IDocumentTransferRelay relay, ICurrentUser currentUser)
    {
        _access = access;
        _relay = relay;
        _currentUser = currentUser;
    }

    [HttpPost]
    public async Task<IActionResult> Register([FromBody] RegisterCustomerDocumentRequest request, CancellationToken cancellationToken)
    {
        var document = await _access.RegisterAsync(
            request.CaseId,
            request.AppointmentId,
            request.FileName,
            request.ContentType,
            request.FileSizeBytes,
            cancellationToken);
        return Ok(new
        {
            documentId = document.Id,
            document.FileName,
            document.ContentType,
            document.FileSizeBytes
        });
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] Guid? caseId, [FromQuery] Guid? appointmentId, CancellationToken cancellationToken)
    {
        var items = await _access.ListAsync(caseId, appointmentId, cancellationToken);
        return Ok(items);
    }

    [HttpPost("{documentId:guid}/requests")]
    public async Task<IActionResult> RequestAccess(Guid documentId, CancellationToken cancellationToken)
    {
        var session = await _access.RequestAsync(documentId, cancellationToken);
        return Ok(new { sessionId = session.Id, status = session.Status.ToString(), expiresAtUtc = session.ExpiresAtUtc });
    }

    [HttpPost("transfers/{sessionId:guid}/approve")]
    public async Task<IActionResult> Approve(Guid sessionId, CancellationToken cancellationToken)
    {
        var approved = await _access.ApproveAsync(sessionId, cancellationToken);
        return Ok(new
        {
            approved.SessionId,
            approved.UploadToken,
            approved.ExpiresAtUtc,
            uploadPath = $"/api/customer-documents/transfers/{approved.SessionId:D}/upload"
        });
    }

    [HttpPost("transfers/{sessionId:guid}/reject")]
    public async Task<IActionResult> Reject(Guid sessionId, CancellationToken cancellationToken)
    {
        await _access.RejectAsync(sessionId, cancellationToken);
        return NoContent();
    }

    [HttpPost("transfers/{sessionId:guid}/upload")]
    [EnableRateLimiting("UploadEndpoints")]
    [RequestSizeLimit(CustomerDocumentRules.MaxBytes)]
    public async Task<IActionResult> Upload(
        Guid sessionId,
        [FromHeader(Name = "X-Transfer-Token")] string? transferToken,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(transferToken))
        {
            return Forbid();
        }

        var session = await _access.BeginUploadAsync(sessionId, _currentUser.GetRequiredUserId(), transferToken.Trim(), cancellationToken);
        var contentType = MediaTypeHeaderValue.TryParse(Request.ContentType, out var parsed)
            ? parsed.MediaType.ToString()
            : string.Empty;
        if (!string.Equals(contentType, session.Document.ContentType, StringComparison.OrdinalIgnoreCase))
        {
            await _access.InterruptAsync(session.Id, CancellationToken.None);
            return BadRequest();
        }

        try
        {
            await _relay.UploadAsync(
                session.Id,
                Request.Body,
                session.Document.FileSizeBytes,
                CustomerDocumentRules.RelayWait,
                HttpContext.RequestAborted);
            await _access.CompleteAsync(session.Id, session.Document.FileSizeBytes, CancellationToken.None);
            return NoContent();
        }
        catch (Exception ex) when (ex is IOException or OperationCanceledException or InvalidOperationException)
        {
            await _access.InterruptAsync(session.Id, CancellationToken.None);
            return Conflict(new { message = "The transfer was interrupted. Ask the customer to approve a new one." });
        }
    }

    [HttpGet("transfers/{sessionId:guid}/download")]
    [EnableRateLimiting("UploadEndpoints")]
    public async Task<IActionResult> Download(Guid sessionId, CancellationToken cancellationToken)
    {
        var session = await _access.BeginDownloadAsync(sessionId, _currentUser.GetRequiredUserId(), cancellationToken);
        Response.ContentType = session.Document.ContentType;
        Response.ContentLength = session.Document.FileSizeBytes;
        Response.Headers.ContentDisposition = $"attachment; filename=\"{session.Document.FileName}\"";
        Response.Headers.CacheControl = "no-store";
        try
        {
            await _relay.DownloadAsync(
                session.Id,
                Response.Body,
                session.Document.FileSizeBytes,
                CustomerDocumentRules.RelayWait,
                HttpContext.RequestAborted);
            return new EmptyResult();
        }
        catch (Exception ex) when (ex is IOException or OperationCanceledException or InvalidOperationException)
        {
            await _access.InterruptAsync(session.Id, CancellationToken.None);
            if (Response.HasStarted)
            {
                HttpContext.Abort();
                return new EmptyResult();
            }

            return Conflict(new { message = "The transfer was interrupted. Ask the customer to approve a new one." });
        }
    }
}

public sealed record RegisterCustomerDocumentRequest(
    Guid? CaseId,
    Guid? AppointmentId,
    string FileName,
    string ContentType,
    long FileSizeBytes);
