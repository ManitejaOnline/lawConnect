#pragma warning disable CS1591
using LawConnect.Domain.Common;
using LawConnect.Domain.Enums;

namespace LawConnect.Domain.Entities;

/// <summary>
/// Refund against a captured or authorized payment.
/// </summary>
public sealed class Refund : BaseEntity
{
    public Guid PaymentId { get; set; }
    public decimal Amount { get; set; }
    public RefundStatus Status { get; set; } = RefundStatus.Pending;
    public string? ProviderRefundId { get; set; }
    public string? Reason { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public DateTime? CompletedAtUtc { get; set; }

    public Payment Payment { get; set; } = null!;
}
