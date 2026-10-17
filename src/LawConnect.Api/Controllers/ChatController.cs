using LawConnect.Api.Extensions;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Chat.Commands.CreateChatUploadUrl;
using LawConnect.Application.Features.Chat.Commands.DeleteMessage;
using LawConnect.Application.Features.Chat.Commands.MarkConversationRead;
using LawConnect.Application.Features.Chat.Commands.SendMessage;
using LawConnect.Application.Features.Chat.Commands.StartConversation;
using LawConnect.Application.Features.Chat.Models;
using LawConnect.Application.Features.Chat.Queries.GetConversations;
using LawConnect.Application.Features.Chat.Queries.GetMessages;
using LawConnect.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace LawConnect.Api.Controllers;

/// <summary>
/// Chat conversation and messaging endpoints.
/// </summary>
[ApiController]
[Authorize]
[Route("api/chat")]
public sealed class ChatController : ControllerBase
{
    private readonly ISender _sender;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChatController"/> class.
    /// </summary>
    public ChatController(ISender sender) => _sender = sender;

    /// <summary>
    /// Starts or returns a one-to-one conversation.
    /// </summary>
    [HttpPost("conversations")]
    [ProducesResponseType(typeof(ConversationListItemResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> StartConversation(
        [FromBody] StartConversationApiRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new StartConversationCommand(request.OtherUserId), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Lists conversations for the current user.
    /// </summary>
    [HttpGet("conversations")]
    [ProducesResponseType(typeof(PagedResult<ConversationListItemResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetConversations(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new GetConversationsQuery(pageNumber, pageSize), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Returns cursor-paginated messages for a conversation.
    /// </summary>
    [HttpGet("conversations/{conversationId:guid}/messages")]
    [ProducesResponseType(typeof(MessageCursorPage), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMessages(
        Guid conversationId,
        [FromQuery] string? cursor,
        [FromQuery] int pageSize = 30,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new GetMessagesQuery(conversationId, cursor, pageSize), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Sends a message.
    /// </summary>
    [HttpPost("conversations/{conversationId:guid}/messages")]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> SendMessage(
        Guid conversationId,
        [FromBody] SendMessageApiRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new SendMessageCommand(
                conversationId,
                request.Body,
                request.Type,
                request.AttachmentStorageKey,
                request.AttachmentFileName,
                request.AttachmentContentType,
                request.AttachmentFileSizeBytes),
            cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Soft-deletes a message.
    /// </summary>
    [HttpDelete("messages/{messageId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteMessage(Guid messageId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new DeleteMessageCommand(messageId), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Marks conversation messages as read.
    /// </summary>
    [HttpPost("conversations/{conversationId:guid}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> MarkRead(
        Guid conversationId,
        [FromBody] MarkReadApiRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new MarkConversationReadCommand(conversationId, request.MessageId),
            cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Creates a signed upload URL for chat attachments.
    /// </summary>
    [HttpPost("uploads/sign")]
    [EnableRateLimiting("UploadEndpoints")]
    [ProducesResponseType(typeof(ChatUploadUrlResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateUploadUrl(
        [FromBody] CreateUploadUrlApiRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new CreateChatUploadUrlCommand(
                request.ConversationId,
                request.FileName,
                request.ContentType,
                request.FileSizeBytes),
            cancellationToken);
        return result.ToActionResult();
    }
}

/// <summary>Start conversation API request.</summary>
public sealed record StartConversationApiRequest(Guid OtherUserId);

/// <summary>Send message API request.</summary>
public sealed record SendMessageApiRequest(
    string? Body,
    MessageType Type,
    string? AttachmentStorageKey,
    string? AttachmentFileName,
    string? AttachmentContentType,
    long? AttachmentFileSizeBytes);

/// <summary>Mark read API request.</summary>
public sealed record MarkReadApiRequest(Guid MessageId);

/// <summary>Create upload URL API request.</summary>
public sealed record CreateUploadUrlApiRequest(
    Guid ConversationId,
    string FileName,
    string ContentType,
    long FileSizeBytes);
