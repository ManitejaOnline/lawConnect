using LawConnect.Domain.Common;

namespace LawConnect.Domain.Entities;

/// <summary>
/// Links a user to a conversation and tracks presence/read state.
/// </summary>
public sealed class ConversationParticipant : BaseEntity
{
    public Guid ConversationId { get; set; }
    public Guid UserId { get; set; }
    public DateTime? LastReadAtUtc { get; set; }
    public DateTime? LastSeenAtUtc { get; set; }
    public bool IsMuted { get; set; }

    public Conversation Conversation { get; set; } = null!;
    public User User { get; set; } = null!;
}
