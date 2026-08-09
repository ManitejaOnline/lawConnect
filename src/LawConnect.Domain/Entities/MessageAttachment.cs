using LawConnect.Domain.Common;

namespace LawConnect.Domain.Entities;

/// <summary>
/// File attachment linked to a chat message.
/// </summary>
public sealed class MessageAttachment : BaseEntity
{
    public Guid MessageId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string StorageKey { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }

    public Message Message { get; set; } = null!;
}
