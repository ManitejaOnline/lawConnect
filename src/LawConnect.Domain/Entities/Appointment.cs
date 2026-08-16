using LawConnect.Domain.Common;
using LawConnect.Domain.Enums;

namespace LawConnect.Domain.Entities;

/// <summary>
/// A scheduled consultation between a customer and a lawyer.
/// </summary>
public sealed class Appointment : BaseEntity
{
    public Guid CustomerId { get; set; }
    public Guid LawyerId { get; set; }
    public DateTime StartUtc { get; set; }
    public DateTime EndUtc { get; set; }
    public AppointmentStatus Status { get; set; } = AppointmentStatus.Scheduled;
    public string? Notes { get; set; }
    public string? CancellationReason { get; set; }
    public DateTime? CancelledAtUtc { get; set; }
    public Guid? CancelledByUserId { get; set; }
    public DateTime? NoShowReportedAtUtc { get; set; }
    public decimal ConsultationFeeSnapshot { get; set; }
    public string TimeZoneId { get; set; } = "Asia/Kolkata";
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;

    public Customer Customer { get; set; } = null!;
    public Lawyer Lawyer { get; set; } = null!;
    public Payment? Payment { get; set; }
}
