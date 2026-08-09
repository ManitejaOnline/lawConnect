using LawConnect.Api.Hubs;
using LawConnect.Application.Common.Realtime;
using LawConnect.Application.Features.Chat.Models;
using Microsoft.AspNetCore.SignalR;

namespace LawConnect.Api.Realtime;

/// <summary>
/// SignalR-backed chat realtime publisher with reconnect-friendly group fan-out.
/// </summary>
public sealed class SignalRChatRealtimePublisher : IChatRealtimePublisher
{
    private readonly IHubContext<ChatHub> _hubContext;
    private readonly IPresenceTracker _presenceTracker;

    /// <summary>
    /// Initializes a new instance of the <see cref="SignalRChatRealtimePublisher"/> class.
    /// </summary>
    public SignalRChatRealtimePublisher(IHubContext<ChatHub> hubContext, IPresenceTracker presenceTracker)
    {
        _hubContext = hubContext;
        _presenceTracker = presenceTracker;
    }

    /// <inheritdoc />
    public Task PublishMessageAsync(
        Guid conversationId,
        MessageResponse message,
        CancellationToken cancellationToken = default) =>
        _hubContext.Clients.Group(ChatHub.GroupName(conversationId))
            .SendAsync("MessageReceived", message, cancellationToken);

    /// <inheritdoc />
    public Task PublishTypingAsync(
        Guid conversationId,
        Guid userId,
        bool isTyping,
        CancellationToken cancellationToken = default) =>
        _hubContext.Clients.Group(ChatHub.GroupName(conversationId))
            .SendAsync("Typing", new { conversationId, userId, isTyping }, cancellationToken);

    /// <inheritdoc />
    public Task PublishReadReceiptAsync(
        Guid conversationId,
        Guid messageId,
        Guid userId,
        DateTime readAtUtc,
        CancellationToken cancellationToken = default) =>
        _hubContext.Clients.Group(ChatHub.GroupName(conversationId))
            .SendAsync("ReadReceipt", new { conversationId, messageId, userId, readAtUtc }, cancellationToken);

    /// <inheritdoc />
    public Task PublishPresenceAsync(
        Guid userId,
        bool isOnline,
        DateTime? lastSeenAtUtc,
        CancellationToken cancellationToken = default) =>
        _hubContext.Clients.All.SendAsync(
            "PresenceChanged",
            new { userId, isOnline, lastSeenAtUtc, connectionCount = _presenceTracker.GetConnections(userId).Count },
            cancellationToken);
}
