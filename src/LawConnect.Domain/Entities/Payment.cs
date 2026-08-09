#pragma warning disable CS1591
using LawConnect.Domain.Common;
using LawConnect.Domain.Enums;

namespace LawConnect.Domain.Entities;

/// <summary>
/// Payment record for appointments and emergency holds.
/// </summary>
public sealed class Payment : BaseEntity
{
    public Guid UserId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "INR";
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public PaymentPurpose Purpose { get; set; }
    public PaymentProviderType Provider { get; set; }
    public string? ProviderOrderId { get; set; }
    public string? ProviderPaymentId { get; set; }
    public Guid? AppointmentId { get; set; }
    public Guid? EmergencyRequestId { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public DateTime? AuthorizedAtUtc { get; set; }
    public DateTime? CapturedAtUtc { get; set; }
    public DateTime? FailedAtUtc { get; set; }
    public string? FailureReason { get; set; }
    public string? MetadataJson { get; set; }

    public User User { get; set; } = null!;
    public Appointment? Appointment { get; set; }
    public EmergencyRequest? EmergencyRequest { get; set; }
    public ICollection<Refund> Refunds { get; set; } = new List<Refund>();
    public Invoice? Invoice { get; set; }
}
