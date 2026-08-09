using LawConnect.Application.Features.Chat.Models;

namespace LawConnect.Application.Common.Realtime;

/// <summary>
/// Publishes chat realtime events to connected clients.
/// </summary>
public interface IChatRealtimePublisher
{
    /// <summary>Publishes a new message to conversation participants.</summary>
    Task PublishMessageAsync(Guid conversationId, MessageResponse message, CancellationToken cancellationToken = default);

    /// <summary>Publishes typing indicator.</summary>
    Task PublishTypingAsync(Guid conversationId, Guid userId, bool isTyping, CancellationToken cancellationToken = default);

    /// <summary>Publishes read receipt.</summary>
    Task PublishReadReceiptAsync(
        Guid conversationId,
        Guid messageId,
        Guid userId,
        DateTime readAtUtc,
        CancellationToken cancellationToken = default);

    /// <summary>Publishes presence change.</summary>
    Task PublishPresenceAsync(Guid userId, bool isOnline, DateTime? lastSeenAtUtc, CancellationToken cancellationToken = default);
}
