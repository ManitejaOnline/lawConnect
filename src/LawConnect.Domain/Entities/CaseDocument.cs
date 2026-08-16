using LawConnect.Domain.Common;

namespace LawConnect.Domain.Entities;

/// <summary>
/// A file attached to a <see cref="Case"/>, optionally scoped to a specific update, hearing, or
/// milestone within it. Reuses the same file storage abstraction as <see cref="LawyerDocument"/>,
/// but is a separate entity since case attachments are not lawyer-verification documents.
/// </summary>
public sealed class CaseDocument : BaseEntity
{
    public Guid CaseId { get; set; }
    public Guid? CaseUpdateId { get; set; }
    public Guid? HearingId { get; set; }
    public Guid? MilestoneId { get; set; }
    public Guid UploadedByUserId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string StorageKey { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }

    public Case Case { get; set; } = null!;
    public CaseUpdate? CaseUpdate { get; set; }
    public Hearing? Hearing { get; set; }
    public CaseMilestone? Milestone { get; set; }
}
