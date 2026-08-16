using LawConnect.Domain.Common;
using LawConnect.Domain.Enums;

namespace LawConnect.Domain.Entities;

/// <summary>
/// An ongoing legal matter a lawyer manages for a customer, distinct from any single
/// <see cref="Appointment"/> consultation.
/// </summary>
public sealed class Case : BaseEntity
{
    public Guid CustomerId { get; set; }
    public Guid LawyerId { get; set; }
    public Guid SpecializationId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? CaseNumber { get; set; }
    public string? Description { get; set; }
    public string? CourtName { get; set; }
    public CaseStatus Status { get; set; } = CaseStatus.Pending;
    public CasePriority Priority { get; set; } = CasePriority.Medium;
    public DateTime StartDate { get; set; }
    public DateTime? ClosedDate { get; set; }

    /// <summary>The appointment that established the lawyer/customer relationship this case was created from, if any.</summary>
    public Guid? SourceAppointmentId { get; set; }

    public Customer Customer { get; set; } = null!;
    public Lawyer Lawyer { get; set; } = null!;
    public Specialization Specialization { get; set; } = null!;
    public Appointment? SourceAppointment { get; set; }

    public ICollection<CaseUpdate> CaseUpdates { get; set; } = new List<CaseUpdate>();
    public ICollection<Hearing> Hearings { get; set; } = new List<Hearing>();
    public ICollection<CaseMilestone> Milestones { get; set; } = new List<CaseMilestone>();
    public ICollection<CaseDocument> Documents { get; set; } = new List<CaseDocument>();
}
