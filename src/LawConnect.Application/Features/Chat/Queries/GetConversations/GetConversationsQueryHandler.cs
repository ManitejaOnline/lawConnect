using FluentValidation;
using LawConnect.Application.Common.Interfaces;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Common.Realtime;
using LawConnect.Application.Features.Chat.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawConnect.Application.Features.Chat.Queries.GetConversations;

/// <summary>
/// Validates <see cref="GetConversationsQuery"/>.
/// </summary>
public sealed class GetConversationsQueryValidator : AbstractValidator<GetConversationsQuery>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetConversationsQueryValidator"/> class.
    /// </summary>
    public GetConversationsQueryValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, PaginationRequest.MaxPageSize);
    }
}

/// <summary>
/// Handles <see cref="GetConversationsQuery"/>.
/// </summary>
public sealed class GetConversationsQueryHandler
    : IRequestHandler<GetConversationsQuery, Result<PagedResult<ConversationListItemResponse>>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IPresenceTracker _presenceTracker;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetConversationsQueryHandler"/> class.
    /// </summary>
    public GetConversationsQueryHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        IPresenceTracker presenceTracker)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _presenceTracker = presenceTracker;
    }

    /// <inheritdoc />
    public async Task<Result<PagedResult<ConversationListItemResponse>>> Handle(
        GetConversationsQuery request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetRequiredUserId();
        var pagination = new PaginationRequest { PageNumber = request.PageNumber, PageSize = request.PageSize };

        var baseQuery = _dbContext.ConversationParticipants
            .AsNoTracking()
            .Where(p => p.UserId == userId);

        var total = await baseQuery.CountAsync(cancellationToken);

        var page = await baseQuery
            .OrderByDescending(p => p.Conversation.LastMessageAtUtc ?? p.CreatedAt)
            .Skip(pagination.Skip)
            .Take(pagination.PageSize)
            .Select(p => new
            {
                p.ConversationId,
                p.LastReadAtUtc,
                LastMessageAtUtc = p.Conversation.LastMessageAtUtc,
                LastMessagePreview = _dbContext.Messages
                    .Where(m => m.ConversationId == p.ConversationId)
                    .OrderByDescending(m => m.SentAtUtc)
                    .Select(m => m.Body)
                    .FirstOrDefault(),
                Other = _dbContext.ConversationParticipants
                    .Where(o => o.ConversationId == p.ConversationId && o.UserId != userId)
                    .Select(o => new
                    {
                        o.UserId,
                        o.User.FirstName,
                        o.User.LastName,
                        o.User.ProfileImageUrl,
                        o.LastSeenAtUtc
                    })
                    .FirstOrDefault(),
                UnreadCount = _dbContext.Messages.Count(m =>
                    m.ConversationId == p.ConversationId
                    && m.SenderUserId != userId
                    && (p.LastReadAtUtc == null || m.SentAtUtc > p.LastReadAtUtc))
            })
            .ToListAsync(cancellationToken);

        var items = page
            .Where(x => x.Other is not null)
            .Select(x => new ConversationListItemResponse(
                x.ConversationId,
                x.Other!.UserId,
                x.Other.FirstName,
                x.Other.LastName,
                x.Other.ProfileImageUrl,
                _presenceTracker.IsOnline(x.Other.UserId),
                x.Other.LastSeenAtUtc,
                x.LastMessagePreview,
                x.LastMessageAtUtc,
                x.UnreadCount))
            .ToList();

        return Result.Success(new PagedResult<ConversationListItemResponse>(
            items,
            total,
            pagination.PageNumber,
            pagination.PageSize));
    }
}
