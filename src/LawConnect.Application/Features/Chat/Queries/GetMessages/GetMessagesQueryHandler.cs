using FluentValidation;
using LawConnect.Application.Common.Interfaces;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Chat.Models;
using LawConnect.Application.Features.Chat.Services;
using LawConnect.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawConnect.Application.Features.Chat.Queries.GetMessages;

/// <summary>
/// Cursor-paginated message history for a conversation.
/// </summary>
public sealed record GetMessagesQuery(
    Guid ConversationId,
    string? Cursor,
    int PageSize) : IRequest<Result<MessageCursorPage>>;

/// <summary>
/// Validates <see cref="GetMessagesQuery"/>.
/// </summary>
public sealed class GetMessagesQueryValidator : AbstractValidator<GetMessagesQuery>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetMessagesQueryValidator"/> class.
    /// </summary>
    public GetMessagesQueryValidator()
    {
        RuleFor(x => x.ConversationId).NotEmpty();
        RuleFor(x => x.PageSize).InclusiveBetween(1, PaginationRequest.MaxPageSize);
    }
}

/// <summary>
/// Handles <see cref="GetMessagesQuery"/>.
/// </summary>
public sealed class GetMessagesQueryHandler : IRequestHandler<GetMessagesQuery, Result<MessageCursorPage>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IConversationAccessService _accessService;
    private readonly IFileStorage _fileStorage;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetMessagesQueryHandler"/> class.
    /// </summary>
    public GetMessagesQueryHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        IConversationAccessService accessService,
        IFileStorage fileStorage)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _accessService = accessService;
        _fileStorage = fileStorage;
    }

    /// <inheritdoc />
    public async Task<Result<MessageCursorPage>> Handle(GetMessagesQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetRequiredUserId();
        await _accessService.EnsureParticipantAsync(request.ConversationId, userId, cancellationToken);

        var query = _dbContext.Messages
            .AsNoTracking()
            .Include(m => m.Attachments)
            .Include(m => m.ReadStatuses)
            .Where(m => m.ConversationId == request.ConversationId);

        if (!string.IsNullOrWhiteSpace(request.Cursor) && Guid.TryParse(request.Cursor, out var cursorId))
        {
            var cursorMessage = await _dbContext.Messages
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == cursorId && m.ConversationId == request.ConversationId, cancellationToken);

            if (cursorMessage is not null)
            {
                query = query.Where(m =>
                    m.SentAtUtc < cursorMessage.SentAtUtc
                    || (m.SentAtUtc == cursorMessage.SentAtUtc && m.Id.CompareTo(cursorId) < 0));
            }
        }

        var rows = await query
            .OrderByDescending(m => m.SentAtUtc)
            .ThenByDescending(m => m.Id)
            .Take(request.PageSize + 1)
            .ToListAsync(cancellationToken);

        var hasMore = rows.Count > request.PageSize;
        var pageRows = hasMore ? rows.Take(request.PageSize).ToList() : rows;

        var items = new List<MessageResponse>();
        foreach (var message in pageRows)
        {
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

            var state = message.ReadStatuses
                .FirstOrDefault(s => s.UserId != message.SenderUserId)?.State.ToString()
                ?? MessageDeliveryState.Sent.ToString();

            items.Add(new MessageResponse(
                message.Id,
                message.ConversationId,
                message.SenderUserId,
                message.Body,
                message.Type.ToString(),
                message.SentAtUtc,
                attachments,
                state));
        }

        var nextCursor = hasMore && pageRows.Count > 0 ? pageRows[^1].Id.ToString("D") : null;
        return Result.Success(new MessageCursorPage(items, nextCursor, hasMore));
    }
}
