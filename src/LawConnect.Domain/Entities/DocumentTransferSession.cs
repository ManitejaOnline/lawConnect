using LawConnect.Domain.Common;
using LawConnect.Domain.Enums;

namespace LawConnect.Domain.Entities;

/// <summary>
/// Authorization and signaling record for one lawyer download attempt.
/// The upload token hash is cleared when the transfer starts, so the token is single-use.
/// </summary>
public sealed class DocumentTransferSession : BaseEntity
{
    public Guid DocumentId { get; set; }

    public Guid LawyerUserId { get; set; }

    public DocumentTransferStatus Status { get; set; } = DocumentTransferStatus.Pending;

    public string? UploadTokenHash { get; set; }

    public DateTime ExpiresAtUtc { get; set; }

    public DateTime? StartedAtUtc { get; set; }

    public DateTime? CompletedAtUtc { get; set; }

    public long? BytesTransferred { get; set; }

    public CustomerDocument Document { get; set; } = null!;

    public User LawyerUser { get; set; } = null!;
}
