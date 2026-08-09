using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Chat.Models;
using MediatR;

namespace LawConnect.Application.Features.Chat.Queries.GetConversations;

/// <summary>
/// Lists conversations for the current user.
/// </summary>
public sealed record GetConversationsQuery(int PageNumber, int PageSize)
    : IRequest<Result<PagedResult<ConversationListItemResponse>>>;
