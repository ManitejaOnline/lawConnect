#pragma warning disable CS1591
using LawConnect.Application.Common.Interfaces;

namespace LawConnect.Application.Common.Interfaces;

/// <summary>
/// Abstract payment provider (Razorpay now; Stripe later).
/// </summary>
public interface IPaymentProvider
{
    /// <summary>Provider name.</summary>
    string Name { get; }

    /// <summary>Creates a gateway order for client checkout.</summary>
    Task<PaymentOrderResult> CreateOrderAsync(
        decimal amount,
        string currency,
        string receipt,
        IReadOnlyDictionary<string, string>? notes = null,
        CancellationToken cancellationToken = default);

    /// <summary>Verifies client payment signature.</summary>
    Task<PaymentVerificationResult> VerifyPaymentSignatureAsync(
        string orderId,
        string paymentId,
        string signature,
        CancellationToken cancellationToken = default);

    /// <summary>Authorizes a hold/authorization without capture.</summary>
    Task<ProviderHoldResult> AuthorizeHoldAsync(
        decimal amount,
        string currency,
        string receipt,
        IReadOnlyDictionary<string, string>? notes = null,
        CancellationToken cancellationToken = default);

    /// <summary>Captures a previously authorized payment.</summary>
    Task<ProviderCaptureResult> CaptureAsync(
        string providerPaymentId,
        decimal amount,
        CancellationToken cancellationToken = default);

    /// <summary>Releases an authorized hold.</summary>
    Task ReleaseHoldAsync(string providerPaymentId, CancellationToken cancellationToken = default);

    /// <summary>Issues a refund against a captured payment.</summary>
    Task<ProviderRefundResult> RefundAsync(
        string providerPaymentId,
        decimal amount,
        string? notes = null,
        CancellationToken cancellationToken = default);

    /// <summary>Verifies webhook signature to prevent replay/forgery.</summary>
    bool VerifyWebhookSignature(string rawBody, string signatureHeader);
}

/// <summary>Hold/authorization result.</summary>
public sealed record ProviderHoldResult(string OrderId, string PaymentId, string Status);

/// <summary>Capture result.</summary>
public sealed record ProviderCaptureResult(string PaymentId, string Status, decimal Amount);

/// <summary>Refund result.</summary>
public sealed record ProviderRefundResult(string RefundId, string Status, decimal Amount);
