using LawConnect.Domain.Common;

namespace LawConnect.Domain.Entities;

/// <summary>
/// Metadata for a document that stays on the customer's phone.
/// The database never stores the file bytes or a local path.
/// </summary>
public sealed class CustomerDocument : BaseEntity
{
    public Guid OwnerUserId { get; set; }

    public Guid? CaseId { get; set; }

    public Guid? AppointmentId { get; set; }

    public string FileName { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public long FileSizeBytes { get; set; }

    public User Owner { get; set; } = null!;

    public Case? Case { get; set; }

    public Appointment? Appointment { get; set; }

    public ICollection<DocumentTransferSession> Transfers { get; set; } = new List<DocumentTransferSession>();
}
