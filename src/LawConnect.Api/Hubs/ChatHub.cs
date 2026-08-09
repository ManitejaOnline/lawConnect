using System.Security.Claims;
using LawConnect.Application.Common.Interfaces;
using LawConnect.Application.Common.Realtime;
using LawConnect.Application.Features.Chat.Commands.MarkConversationRead;
using LawConnect.Application.Features.Chat.Commands.MarkMessageDelivered;
using LawConnect.Application.Features.Chat.Commands.SendMessage;
using LawConnect.Application.Features.Chat.Services;
using LawConnect.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace LawConnect.Api.Hubs;

/// <summary>
/// Realtime chat hub for messages, typing, presence, and receipts.
/// </summary>
[Authorize]
public sealed class ChatHub : Hub
{
    private readonly ISender _sender;
    private readonly IPresenceTracker _presenceTracker;
    private readonly IChatRealtimePublisher _realtimePublisher;
    private readonly IConversationAccessService _accessService;
    private readonly IApplicationDbContext _dbContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChatHub"/> class.
    /// </summary>
    public ChatHub(
        ISender sender,
        IPresenceTracker presenceTracker,
        IChatRealtimePublisher realtimePublisher,
        IConversationAccessService accessService,
        IApplicationDbContext dbContext,
        IDateTimeProvider dateTimeProvider)
    {
        _sender = sender;
        _presenceTracker = presenceTracker;
        _realtimePublisher = realtimePublisher;
        _accessService = accessService;
        _dbContext = dbContext;
        _dateTimeProvider = dateTimeProvider;
    }

    /// <inheritdoc />
    public override async Task OnConnectedAsync()
    {
        var userId = GetUserId();
        await _presenceTracker.UserConnectedAsync(userId, Context.ConnectionId);

        var participant = await _dbContext.ConversationParticipants
            .Where(p => p.UserId == userId)
            .ToListAsync();
        foreach (var item in participant)
        {
            item.LastSeenAtUtc = _dateTimeProvider.UtcNow;
        }

        if (participant.Count > 0)
        {
            await _dbContext.SaveChangesAsync();
        }

        await _realtimePublisher.PublishPresenceAsync(userId, true, _dateTimeProvider.UtcNow);
        await base.OnConnectedAsync();
    }

    /// <inheritdoc />
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetUserId();
        await _presenceTracker.UserDisconnectedAsync(userId, Context.ConnectionId);

        if (!_presenceTracker.IsOnline(userId))
        {
            var now = _dateTimeProvider.UtcNow;
            var rows = await _dbContext.ConversationParticipants.Where(p => p.UserId == userId).ToListAsync();
            foreach (var row in rows)
            {
                row.LastSeenAtUtc = now;
            }

            if (rows.Count > 0)
            {
                await _dbContext.SaveChangesAsync();
            }

            await _realtimePublisher.PublishPresenceAsync(userId, false, now);
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Joins a conversation group after ownership validation.
    /// </summary>
    public async Task JoinConversation(Guid conversationId)
    {
        var userId = GetUserId();
        await _accessService.EnsureParticipantAsync(conversationId, userId);
        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(conversationId));
    }

    /// <summary>
    /// Leaves a conversation group.
    /// </summary>
    public Task LeaveConversation(Guid conversationId) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(conversationId));

    /// <summary>
    /// Sends a text message through CQRS.
    /// </summary>
    public async Task SendMessage(Guid conversationId, string body)
    {
        await _sender.Send(new SendMessageCommand(
            conversationId,
            body,
            MessageType.Text,
            null,
            null,
            null,
            null));
    }

    /// <summary>
    /// Broadcasts typing indicator.
    /// </summary>
    public async Task Typing(Guid conversationId, bool isTyping)
    {
        var userId = GetUserId();
        await _accessService.EnsureParticipantAsync(conversationId, userId);
        await _realtimePublisher.PublishTypingAsync(conversationId, userId, isTyping);
    }

    /// <summary>
    /// Marks a message delivered.
    /// </summary>
    public Task MarkDelivered(Guid messageId) =>
        _sender.Send(new MarkMessageDeliveredCommand(messageId));

    /// <summary>
    /// Marks conversation read up to a message.
    /// </summary>
    public Task MarkRead(Guid conversationId, Guid messageId) =>
        _sender.Send(new MarkConversationReadCommand(conversationId, messageId));

    private Guid GetUserId()
    {
        var raw = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? Context.User?.FindFirstValue("sub");
        return Guid.TryParse(raw, out var id)
            ? id
            : throw new HubException("Authentication is required.");
    }

    internal static string GroupName(Guid conversationId) => $"conversation:{conversationId:D}";
}
