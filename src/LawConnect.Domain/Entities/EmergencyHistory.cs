using LawConnect.Domain.Common;
using LawConnect.Domain.Enums;

namespace LawConnect.Domain.Entities;

/// <summary>
/// Terminal snapshot of an SOS request for history and metrics.
/// </summary>
public sealed class EmergencyHistory : BaseEntity
{
    public Guid EmergencyRequestId { get; set; }
    public Guid CustomerId { get; set; }
    public Guid CategoryId { get; set; }
    public Guid? LawyerId { get; set; }
    public EmergencyStatus FinalStatus { get; set; }
    public int AttemptCount { get; set; }
    public double? AcceptanceTimeSeconds { get; set; }
    public double? ResponseTimeSeconds { get; set; }
    public string? CancellationReason { get; set; }
    public DateTime ClosedAtUtc { get; set; }

    public EmergencyRequest EmergencyRequest { get; set; } = null!;
}
