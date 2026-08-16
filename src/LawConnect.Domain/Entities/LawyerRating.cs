using LawConnect.Domain.Common;

namespace LawConnect.Domain.Entities;

/// <summary>
/// A 1-5 star rating a customer leaves for a lawyer. Linked to exactly one of
/// <see cref="AppointmentId"/> (a completed consultation) or <see cref="CaseId"/> (a
/// completed/closed case) — never both, never neither. Which one is set determines the
/// eligibility rule enforced by the submitting command handler.
/// </summary>
public sealed class LawyerRating : BaseEntity
{
    public Guid LawyerId { get; set; }
    public Guid ClientId { get; set; }
    public Guid? AppointmentId { get; set; }
    public Guid? CaseId { get; set; }
    public int Stars { get; set; }
    public string? ReviewText { get; set; }

    public Lawyer Lawyer { get; set; } = null!;
    public Customer Client { get; set; } = null!;
    public Appointment? Appointment { get; set; }
    public Case? Case { get; set; }
}
