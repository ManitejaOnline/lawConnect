using LawConnect.Domain.Common;
using LawConnect.Domain.Enums;

namespace LawConnect.Domain.Entities;

/// <summary>
/// A chat message within a conversation.
/// </summary>
public sealed class Message : BaseEntity
{
    public Guid ConversationId { get; set; }
    public Guid SenderUserId { get; set; }
    public string? Body { get; set; }
    public MessageType Type { get; set; } = MessageType.Text;
    public DateTime SentAtUtc { get; set; }

    public Conversation Conversation { get; set; } = null!;
    public User Sender { get; set; } = null!;
    public ICollection<MessageAttachment> Attachments { get; set; } = new List<MessageAttachment>();
    public ICollection<MessageReadStatus> ReadStatuses { get; set; } = new List<MessageReadStatus>();
}
