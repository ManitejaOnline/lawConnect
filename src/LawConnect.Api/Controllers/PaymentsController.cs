#pragma warning disable CS1591
using System.Text;
using LawConnect.Api.Extensions;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Payments.Commands;
using LawConnect.Application.Features.Payments.Models;
using LawConnect.Application.Features.Payments.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LawConnect.Api.Controllers;

/// <summary>
/// Payments, wallet, refunds, invoices, and webhooks.
/// </summary>
[ApiController]
[Authorize]
[Route("api/payments")]
public sealed class PaymentsController : ControllerBase
{
    private readonly ISender _sender;

    /// <summary>
    /// Initializes a new instance of the <see cref="PaymentsController"/> class.
    /// </summary>
    public PaymentsController(ISender sender) => _sender = sender;

    /// <summary>
    /// Initiates appointment payment checkout.
    /// </summary>
    [HttpPost("appointments/{appointmentId:guid}/initiate")]
    [ProducesResponseType(typeof(PaymentResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> InitiateAppointment(
        Guid appointmentId,
        [FromBody] IdempotentPaymentApiRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new InitiateAppointmentPaymentCommand(appointmentId, request.IdempotencyKey),
            cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Confirms appointment payment after provider checkout.
    /// </summary>
    [HttpPost("{paymentId:guid}/confirm")]
    [ProducesResponseType(typeof(PaymentResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Confirm(
        Guid paymentId,
        [FromBody] ConfirmPaymentApiRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new ConfirmAppointmentPaymentCommand(
                paymentId,
                request.ProviderPaymentId,
                request.Signature,
                request.IdempotencyKey),
            cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Refunds a captured payment.
    /// </summary>
    [HttpPost("{paymentId:guid}/refund")]
    [ProducesResponseType(typeof(RefundResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Refund(
        Guid paymentId,
        [FromBody] RefundPaymentApiRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new RefundPaymentCommand(paymentId, request.Amount, request.Reason, request.IdempotencyKey),
            cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Gets invoice for a payment.
    /// </summary>
    [HttpGet("{paymentId:guid}/invoice")]
    [ProducesResponseType(typeof(InvoiceResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Invoice(Guid paymentId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetInvoiceByPaymentQuery(paymentId), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Gets the current user's wallet.
    /// </summary>
    [HttpGet("wallet")]
    [ProducesResponseType(typeof(WalletResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Wallet(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetMyWalletQuery(), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Tops up the current user's wallet.
    /// </summary>
    [HttpPost("wallet/top-up")]
    [ProducesResponseType(typeof(WalletResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> TopUp(
        [FromBody] TopUpWalletApiRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new TopUpWalletCommand(request.Amount, request.IdempotencyKey), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Lists wallet transaction history.
    /// </summary>
    [HttpGet("wallet/transactions")]
    [ProducesResponseType(typeof(PagedResult<WalletTransactionResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Transactions(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new GetMyWalletTransactionsQuery(pageNumber, pageSize), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Razorpay/Fake webhook endpoint (signature verified).
    /// </summary>
    [AllowAnonymous]
    [HttpPost("webhooks/{provider}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Webhook(
        string provider,
        [FromHeader(Name = "X-Razorpay-Signature")] string? razorpaySignature,
        [FromHeader(Name = "X-Payment-Signature")] string? fakeSignature,
        CancellationToken cancellationToken)
    {
        Request.EnableBuffering();
        using var reader = new StreamReader(Request.Body, Encoding.UTF8, leaveOpen: true);
        var rawBody = await reader.ReadToEndAsync(cancellationToken);
        Request.Body.Position = 0;

        var signature = razorpaySignature ?? fakeSignature ?? string.Empty;
        string eventId;
        string eventType;
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(string.IsNullOrWhiteSpace(rawBody) ? "{}" : rawBody);
            eventId = doc.RootElement.TryGetProperty("event_id", out var idNode)
                ? idNode.GetString() ?? Guid.NewGuid().ToString("N")
                : Guid.NewGuid().ToString("N");
            eventType = doc.RootElement.TryGetProperty("event", out var typeNode)
                ? typeNode.GetString() ?? "unknown"
                : "unknown";
        }
        catch
        {
            eventId = Guid.NewGuid().ToString("N");
            eventType = "unknown";
        }

        var result = await _sender.Send(
            new ProcessPaymentWebhookCommand(provider, eventId, eventType, rawBody, signature),
            cancellationToken);
        return result.ToActionResult();
    }
}

/// <summary>Idempotent request envelope.</summary>
public sealed record IdempotentPaymentApiRequest(string IdempotencyKey);

/// <summary>Confirm payment request.</summary>
public sealed record ConfirmPaymentApiRequest(string ProviderPaymentId, string Signature, string IdempotencyKey);

/// <summary>Refund request.</summary>
public sealed record RefundPaymentApiRequest(decimal? Amount, string? Reason, string IdempotencyKey);

/// <summary>Wallet top-up request.</summary>
public sealed record TopUpWalletApiRequest(decimal Amount, string IdempotencyKey);
