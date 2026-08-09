#pragma warning disable CS1591
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using LawConnect.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace LawConnect.Infrastructure.Payments;

/// <summary>
/// Deterministic in-memory payment provider for local/testing.
/// </summary>
public sealed class FakePaymentProvider : IPaymentProvider, IPaymentGateway
{
    private readonly ConcurrentDictionary<string, FakePayment> _payments = new();
    private readonly ILogger<FakePaymentProvider> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="FakePaymentProvider"/> class.
    /// </summary>
    public FakePaymentProvider(ILogger<FakePaymentProvider> logger) => _logger = logger;

    /// <inheritdoc />
    public string Name => "Fake";

    /// <inheritdoc />
    public Task<PaymentOrderResult> CreateOrderAsync(
        decimal amount,
        string currency,
        string receipt,
        IReadOnlyDictionary<string, string>? notes = null,
        CancellationToken cancellationToken = default)
    {
        var orderId = $"order_fake_{Guid.NewGuid():N}";
        _payments[orderId] = new FakePayment(orderId, null, amount, currency, "created");
        return Task.FromResult(new PaymentOrderResult(orderId, amount, currency, "created"));
    }

    /// <inheritdoc />
    public Task<PaymentVerificationResult> VerifyPaymentAsync(
        string orderId,
        string paymentId,
        string signature,
        CancellationToken cancellationToken = default) =>
        VerifyPaymentSignatureAsync(orderId, paymentId, signature, cancellationToken);

    /// <inheritdoc />
    public Task<PaymentVerificationResult> VerifyPaymentSignatureAsync(
        string orderId,
        string paymentId,
        string signature,
        CancellationToken cancellationToken = default)
    {
        var expected = ComputeSignature(orderId, paymentId);
        var valid = string.Equals(expected, signature, StringComparison.Ordinal)
                    || string.Equals(signature, "fake-valid-signature", StringComparison.Ordinal);
        if (valid && _payments.TryGetValue(orderId, out var payment))
        {
            _payments[orderId] = payment with { PaymentId = paymentId, Status = "authorized" };
        }

        return Task.FromResult(new PaymentVerificationResult(valid, paymentId, orderId));
    }

    /// <inheritdoc />
    public Task<ProviderHoldResult> AuthorizeHoldAsync(
        decimal amount,
        string currency,
        string receipt,
        IReadOnlyDictionary<string, string>? notes = null,
        CancellationToken cancellationToken = default)
    {
        var orderId = $"order_hold_{Guid.NewGuid():N}";
        var paymentId = $"pay_hold_{Guid.NewGuid():N}";
        _payments[paymentId] = new FakePayment(orderId, paymentId, amount, currency, "authorized");
        _logger.LogInformation("Fake hold {PaymentId} amount {Amount}", paymentId, amount);
        return Task.FromResult(new ProviderHoldResult(orderId, paymentId, "authorized"));
    }

    /// <inheritdoc />
    public Task<ProviderCaptureResult> CaptureAsync(
        string providerPaymentId,
        decimal amount,
        CancellationToken cancellationToken = default)
    {
        if (!_payments.TryGetValue(providerPaymentId, out var payment)
            && !_payments.Values.Any(p => p.PaymentId == providerPaymentId))
        {
            payment = new FakePayment("order_unknown", providerPaymentId, amount, "INR", "authorized");
        }
        else if (!_payments.TryGetValue(providerPaymentId, out payment))
        {
            payment = _payments.Values.First(p => p.PaymentId == providerPaymentId);
        }

        var captured = payment with { Status = "captured" };
        _payments[providerPaymentId] = captured;
        return Task.FromResult(new ProviderCaptureResult(providerPaymentId, "captured", amount));
    }

    /// <inheritdoc />
    public Task ReleaseHoldAsync(string providerPaymentId, CancellationToken cancellationToken = default)
    {
        if (_payments.TryGetValue(providerPaymentId, out var payment))
        {
            _payments[providerPaymentId] = payment with { Status = "released" };
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<ProviderRefundResult> RefundAsync(
        string providerPaymentId,
        decimal amount,
        string? notes = null,
        CancellationToken cancellationToken = default)
    {
        var refundId = $"rfnd_fake_{Guid.NewGuid():N}";
        return Task.FromResult(new ProviderRefundResult(refundId, "processed", amount));
    }

    /// <inheritdoc />
    public bool VerifyWebhookSignature(string rawBody, string signatureHeader) =>
        string.Equals(signatureHeader, "fake-webhook-signature", StringComparison.Ordinal)
        || string.Equals(signatureHeader, ComputeHmac(rawBody, "fake-webhook-secret"), StringComparison.Ordinal);

    /// <summary>Computes a deterministic client signature for tests.</summary>
    public static string ComputeSignature(string orderId, string paymentId) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{orderId}|{paymentId}"))).ToLowerInvariant();

    private static string ComputeHmac(string body, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(body))).ToLowerInvariant();
    }

    private sealed record FakePayment(string OrderId, string? PaymentId, decimal Amount, string Currency, string Status);
}
