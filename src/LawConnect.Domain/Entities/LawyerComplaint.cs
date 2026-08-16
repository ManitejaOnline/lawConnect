using LawConnect.Domain.Common;
using LawConnect.Domain.Enums;

namespace LawConnect.Domain.Entities;

public sealed class LawyerComplaint : BaseEntity
{
    public Guid LawyerId { get; set; }
    public Guid ClientId { get; set; }
    public Guid? AppointmentId { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ComplaintStatus Status { get; set; } = ComplaintStatus.Pending;
    public ComplaintSeverity? Severity { get; set; }
    public string? AdminNotes { get; set; }
    public DateTime? ReviewedAtUtc { get; set; }
    public Guid? ReviewedByUserId { get; set; }

    public Lawyer Lawyer { get; set; } = null!;
    public Customer Client { get; set; } = null!;
    public Appointment? Appointment { get; set; }
}
