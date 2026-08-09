#pragma warning disable CS1591
using LawConnect.Domain.Common;

namespace LawConnect.Domain.Entities;

/// <summary>
/// Invoice generated for a successful payment.
/// </summary>
public sealed class Invoice : BaseEntity
{
    public string InvoiceNumber { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public Guid PaymentId { get; set; }
    public decimal Amount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "INR";
    public DateTime IssuedAtUtc { get; set; }
    public string? LineItemsJson { get; set; }

    public User User { get; set; } = null!;
    public Payment Payment { get; set; } = null!;
}
