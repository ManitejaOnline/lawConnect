#pragma warning disable CS1591
using System.ComponentModel.DataAnnotations;

namespace LawConnect.Shared.Options;

/// <summary>
/// Payment provider configuration.
/// </summary>
public sealed class PaymentOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Payments";

    /// <summary>Active provider: Razorpay | Fake.</summary>
    [Required]
    public string Provider { get; set; } = "Fake";

    /// <summary>Default currency.</summary>
    [Required]
    public string Currency { get; set; } = "INR";

    /// <summary>Idempotency record TTL in hours.</summary>
    [Range(1, 168)]
    public int IdempotencyTtlHours { get; set; } = 24;

    /// <summary>Outbox max publish attempts.</summary>
    [Range(1, 20)]
    public int OutboxMaxAttempts { get; set; } = 5;

    /// <summary>Razorpay settings.</summary>
    public RazorpayOptions Razorpay { get; set; } = new();
}

/// <summary>
/// Razorpay credentials and webhook secrets.
/// </summary>
public sealed class RazorpayOptions
{
    public string KeyId { get; set; } = string.Empty;
    public string KeySecret { get; set; } = string.Empty;
    public string WebhookSecret { get; set; } = string.Empty;
}
