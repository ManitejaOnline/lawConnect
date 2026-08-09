namespace LawConnect.Application.Features.Chat.Models;

/// <summary>
/// Conversation list item.
/// </summary>
public sealed record ConversationListItemResponse(
    Guid ConversationId,
    Guid OtherUserId,
    string OtherFirstName,
    string OtherLastName,
    string? OtherProfileImageUrl,
    bool OtherIsOnline,
    DateTime? OtherLastSeenAtUtc,
    string? LastMessagePreview,
    DateTime? LastMessageAtUtc,
    int UnreadCount);

/// <summary>
/// Chat message response.
/// </summary>
public sealed record MessageResponse(
    Guid MessageId,
    Guid ConversationId,
    Guid SenderUserId,
    string? Body,
    string Type,
    DateTime SentAtUtc,
    IReadOnlyList<AttachmentResponse> Attachments,
    string DeliveryState);

/// <summary>
/// Attachment response.
/// </summary>
public sealed record AttachmentResponse(
    Guid AttachmentId,
    string FileName,
    string ContentType,
    long FileSizeBytes,
    string? DownloadUrl);

/// <summary>
/// Cursor page of messages.
/// </summary>
public sealed record MessageCursorPage(
    IReadOnlyList<MessageResponse> Items,
    string? NextCursor,
    bool HasMore);

/// <summary>
/// Signed upload response.
/// </summary>
public sealed record ChatUploadUrlResponse(
    string StorageKey,
    string UploadUrl,
    DateTime ExpiresAtUtc);

/// <summary>
/// Notification list item.
/// </summary>
public sealed record NotificationItemResponse(
    Guid Id,
    string Title,
    string Body,
    string Type,
    string Channel,
    bool IsRead,
    DateTime CreatedAt,
    string? DataJson);
