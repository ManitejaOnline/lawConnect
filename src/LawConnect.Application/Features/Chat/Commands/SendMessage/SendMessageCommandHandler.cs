using System.Text.Json;
using LawConnect.Application.Common.Interfaces;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Common.Realtime;
using LawConnect.Application.Features.Chat.Models;
using LawConnect.Application.Features.Chat.Services;
using LawConnect.Domain.Entities;
using LawConnect.Domain.Enums;
using LawConnect.Domain.Exceptions;
using LawConnect.Shared.Constants;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LawConnect.Application.Features.Chat.Commands.SendMessage;

/// <summary>
/// Handles <see cref="SendMessageCommand"/>.
/// </summary>
public sealed class SendMessageCommandHandler : IRequestHandler<SendMessageCommand, Result<MessageResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IConversationAccessService _accessService;
    private readonly IChatRealtimePublisher _realtimePublisher;
    private readonly IUserNotificationDispatcher _notificationDispatcher;
    private readonly IFileStorage _fileStorage;
    private readonly ILogger<SendMessageCommandHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SendMessageCommandHandler"/> class.
    /// </summary>
    public SendMessageCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider,
        IConversationAccessService accessService,
        IChatRealtimePublisher realtimePublisher,
        IUserNotificationDispatcher notificationDispatcher,
        IFileStorage fileStorage,
        ILogger<SendMessageCommandHandler> logger)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
        _accessService = accessService;
        _realtimePublisher = realtimePublisher;
        _notificationDispatcher = notificationDispatcher;
        _fileStorage = fileStorage;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<MessageResponse>> Handle(SendMessageCommand request, CancellationToken cancellationToken)
    {
        var senderId = _currentUser.GetRequiredUserId();
        await _accessService.EnsureParticipantAsync(request.ConversationId, senderId, cancellationToken);

        var recipientId = await _accessService.GetOtherParticipantUserIdAsync(
            request.ConversationId,
            senderId,
            cancellationToken);

        var now = _dateTimeProvider.UtcNow;
        var message = new Message
        {
            ConversationId = request.ConversationId,
            SenderUserId = senderId,
            Body = string.IsNullOrWhiteSpace(request.Body) ? null : request.Body.Trim(),
            Type = request.Type,
            SentAtUtc = now
        };

        if (request.Type != MessageType.Text)
        {
            message.Attachments.Add(new MessageAttachment
            {
                FileName = request.AttachmentFileName!.Trim(),
                StorageKey = request.AttachmentStorageKey!.Trim(),
                ContentType = request.AttachmentContentType!.Trim(),
                FileSizeBytes = request.AttachmentFileSizeBytes!.Value
            });
        }

        message.ReadStatuses.Add(new MessageReadStatus
        {
            UserId = recipientId,
            State = MessageDeliveryState.Sent
        });

        _dbContext.Messages.Add(message);

        var conversation = await _dbContext.Conversations
            .FirstAsync(c => c.Id == request.ConversationId, cancellationToken);
        conversation.LastMessageAtUtc = now;

        await _dbContext.SaveChangesAsync(cancellationToken);
        conversation.LastMessageId = message.Id;
        await _dbContext.SaveChangesAsync(cancellationToken);

        var response = await MapMessageAsync(message.Id, cancellationToken);
        await _realtimePublisher.PublishMessageAsync(request.ConversationId, response, cancellationToken);

        var preview = request.Type == MessageType.Text
            ? message.Body ?? string.Empty
            : $"Sent a {request.Type.ToString().ToLowerInvariant()}";

        await _notificationDispatcher.NotifyUserAsync(
            recipientId,
            "New message",
            preview,
            NotificationTypes.NewMessage,
            new Dictionary<string, string>
            {
                ["conversationId"] = request.ConversationId.ToString("D"),
                ["messageId"] = message.Id.ToString("D")
            },
            cancellationToken);

        _logger.LogInformation(
            "Message {MessageId} sent in conversation {ConversationId}",
            message.Id,
            request.ConversationId);

        return Result.Success(response);
    }

    private async Task<MessageResponse> MapMessageAsync(Guid messageId, CancellationToken cancellationToken)
    {
        var message = await _dbContext.Messages
            .AsNoTracking()
            .Include(m => m.Attachments)
            .Include(m => m.ReadStatuses)
            .FirstAsync(m => m.Id == messageId, cancellationToken);

        var attachments = new List<AttachmentResponse>();
        foreach (var attachment in message.Attachments)
        {
            var url = await _fileStorage.GetPresignedUrlAsync(
                attachment.StorageKey,
                TimeSpan.FromHours(1),
                cancellationToken);
            attachments.Add(new AttachmentResponse(
                attachment.Id,
                attachment.FileName,
                attachment.ContentType,
                attachment.FileSizeBytes,
                url.IsAbsoluteUri ? url.ToString() : url.OriginalString));
        }

        var state = message.ReadStatuses.FirstOrDefault()?.State.ToString() ?? MessageDeliveryState.Sent.ToString();
        return new MessageResponse(
            message.Id,
            message.ConversationId,
            message.SenderUserId,
            message.Body,
            message.Type.ToString(),
            message.SentAtUtc,
            attachments,
            state);
    }
}
