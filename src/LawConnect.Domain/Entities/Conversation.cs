using LawConnect.Domain.Common;

namespace LawConnect.Domain.Entities;

/// <summary>
/// One-to-one chat conversation.
/// </summary>
public sealed class Conversation : BaseEntity
{
    public DateTime? LastMessageAtUtc { get; set; }
    public Guid? LastMessageId { get; set; }

    public ICollection<ConversationParticipant> Participants { get; set; } = new List<ConversationParticipant>();
    public ICollection<Message> Messages { get; set; } = new List<Message>();
}
