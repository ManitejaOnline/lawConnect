using LawConnect.Domain.Common;
using LawConnect.Domain.Enums;

namespace LawConnect.Domain.Entities;

/// <summary>
/// Lawyer notification/assignment for a specific SOS attempt.
/// </summary>
public sealed class EmergencyAssignment : BaseEntity
{
    public Guid EmergencyRequestId { get; set; }
    public Guid LawyerId { get; set; }
    public int AttemptNumber { get; set; }
    public int Rank { get; set; }
    public double DistanceKm { get; set; }
    public double Score { get; set; }
    public EmergencyAssignmentStatus Status { get; set; } = EmergencyAssignmentStatus.Pending;
    public DateTime NotifiedAtUtc { get; set; }
    public DateTime? RespondedAtUtc { get; set; }
    public double? ResponseTimeSeconds { get; set; }
    public string? CancellationReason { get; set; }

    public EmergencyRequest EmergencyRequest { get; set; } = null!;
    public Lawyer Lawyer { get; set; } = null!;
}
