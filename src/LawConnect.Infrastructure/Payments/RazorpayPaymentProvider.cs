#pragma warning disable CS1591
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using LawConnect.Application.Common.Interfaces;
using LawConnect.Shared.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LawConnect.Infrastructure.Payments;

/// <summary>
/// Razorpay payment provider implementation.
/// </summary>
public sealed class RazorpayPaymentProvider : IPaymentProvider, IPaymentGateway
{
    private readonly HttpClient _httpClient;
    private readonly PaymentOptions _options;
    private readonly ILogger<RazorpayPaymentProvider> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RazorpayPaymentProvider"/> class.
    /// </summary>
    public RazorpayPaymentProvider(
        HttpClient httpClient,
        IOptions<PaymentOptions> options,
        ILogger<RazorpayPaymentProvider> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
        var credentials = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{_options.Razorpay.KeyId}:{_options.Razorpay.KeySecret}"));
        _httpClient.BaseAddress ??= new Uri("https://api.razorpay.com/v1/");
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
    }

    /// <inheritdoc />
    public string Name => "Razorpay";

    /// <inheritdoc />
    public async Task<PaymentOrderResult> CreateOrderAsync(
        decimal amount,
        string currency,
        string receipt,
        IReadOnlyDictionary<string, string>? notes = null,
        CancellationToken cancellationToken = default)
    {
        var payload = new Dictionary<string, object?>
        {
            ["amount"] = ToPaise(amount),
            ["currency"] = currency,
            ["receipt"] = receipt,
            ["notes"] = notes ?? new Dictionary<string, string>()
        };

        using var response = await _httpClient.PostAsync(
            "orders",
            new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"),
            cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Razorpay create order failed: {Body}", body);
            throw new InvalidOperationException("Failed to create Razorpay order.");
        }

        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;
        return new PaymentOrderResult(
            root.GetProperty("id").GetString()!,
            amount,
            currency,
            root.GetProperty("status").GetString() ?? "created");
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
        var payload = $"{orderId}|{paymentId}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_options.Razorpay.KeySecret));
        var hash = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();
        var valid = string.Equals(hash, signature, StringComparison.OrdinalIgnoreCase);
        return Task.FromResult(new PaymentVerificationResult(valid, paymentId, orderId));
    }

    /// <inheritdoc />
    public async Task<ProviderHoldResult> AuthorizeHoldAsync(
        decimal amount,
        string currency,
        string receipt,
        IReadOnlyDictionary<string, string>? notes = null,
        CancellationToken cancellationToken = default)
    {
        // Razorpay authorization: create order then a payment link/auth is client-driven.
        // For server-side hold simulation with capture later, create order marked for auth.
        var order = await CreateOrderAsync(amount, currency, receipt, notes, cancellationToken);
        var paymentId = $"pending_auth_{order.OrderId}";
        return new ProviderHoldResult(order.OrderId, paymentId, "authorized");
    }

    /// <inheritdoc />
    public async Task<ProviderCaptureResult> CaptureAsync(
        string providerPaymentId,
        decimal amount,
        CancellationToken cancellationToken = default)
    {
        var payload = new Dictionary<string, object?>
        {
            ["amount"] = ToPaise(amount),
            ["currency"] = _options.Currency
        };

        using var response = await _httpClient.PostAsync(
            $"payments/{providerPaymentId}/capture",
            new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"),
            cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Razorpay capture failed: {Body}", body);
            throw new InvalidOperationException("Failed to capture Razorpay payment.");
        }

        using var doc = JsonDocument.Parse(body);
        return new ProviderCaptureResult(
            providerPaymentId,
            doc.RootElement.GetProperty("status").GetString() ?? "captured",
            amount);
    }

    /// <inheritdoc />
    public async Task ReleaseHoldAsync(string providerPaymentId, CancellationToken cancellationToken = default)
    {
        // Razorpay releases unused authorizations by refunding authorized payments.
        if (providerPaymentId.StartsWith("pending_auth_", StringComparison.Ordinal))
        {
            return;
        }

        await RefundAsync(providerPaymentId, 0, "release_hold", cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ProviderRefundResult> RefundAsync(
        string providerPaymentId,
        decimal amount,
        string? notes = null,
        CancellationToken cancellationToken = default)
    {
        var payload = new Dictionary<string, object?>
        {
            ["amount"] = amount <= 0 ? null : ToPaise(amount),
            ["notes"] = new Dictionary<string, string> { ["reason"] = notes ?? "refund" }
        };

        using var response = await _httpClient.PostAsync(
            $"payments/{providerPaymentId}/refund",
            new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"),
            cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Razorpay refund failed: {Body}", body);
            throw new InvalidOperationException("Failed to refund Razorpay payment.");
        }

        using var doc = JsonDocument.Parse(body);
        return new ProviderRefundResult(
            doc.RootElement.GetProperty("id").GetString()!,
            doc.RootElement.GetProperty("status").GetString() ?? "processed",
            amount);
    }

    /// <inheritdoc />
    public bool VerifyWebhookSignature(string rawBody, string signatureHeader)
    {
        if (string.IsNullOrWhiteSpace(_options.Razorpay.WebhookSecret))
        {
            return false;
        }

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_options.Razorpay.WebhookSecret));
        var hash = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(rawBody))).ToLowerInvariant();
        return string.Equals(hash, signatureHeader, StringComparison.OrdinalIgnoreCase);
    }

    private static long ToPaise(decimal amount) => (long)Math.Round(amount * 100m, MidpointRounding.AwayFromZero);
}
