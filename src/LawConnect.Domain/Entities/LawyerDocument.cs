using LawConnect.Domain.Common;
using LawConnect.Domain.Enums;

namespace LawConnect.Domain.Entities;

public sealed class LawyerDocument : BaseEntity
{
    public Guid LawyerId { get; set; }
    public DocumentType DocumentType { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string StorageKey { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public DocumentVerificationStatus VerificationStatus { get; set; } = DocumentVerificationStatus.Pending;
    public string? ReviewNotes { get; set; }
    public DateTime? ReviewedAtUtc { get; set; }
    public Guid? ReviewedByUserId { get; set; }

    public Lawyer Lawyer { get; set; } = null!;
}
