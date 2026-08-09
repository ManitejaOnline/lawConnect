using LawConnect.Application.Common.Interfaces;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Common.Realtime;
using LawConnect.Application.Features.Chat.Models;
using LawConnect.Domain.Entities;
using LawConnect.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawConnect.Application.Features.Chat.Commands.StartConversation;

/// <summary>
/// Handles <see cref="StartConversationCommand"/>.
/// </summary>
public sealed class StartConversationCommandHandler
    : IRequestHandler<StartConversationCommand, Result<ConversationListItemResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IPresenceTracker _presenceTracker;

    /// <summary>
    /// Initializes a new instance of the <see cref="StartConversationCommandHandler"/> class.
    /// </summary>
    public StartConversationCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        IPresenceTracker presenceTracker)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _presenceTracker = presenceTracker;
    }

    /// <inheritdoc />
    public async Task<Result<ConversationListItemResponse>> Handle(
        StartConversationCommand request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetRequiredUserId();
        if (request.OtherUserId == userId)
        {
            throw new BusinessRuleException("Cannot start a conversation with yourself.", "Chat.InvalidParticipant");
        }

        var otherExists = await _dbContext.Users.AnyAsync(u => u.Id == request.OtherUserId, cancellationToken);
        if (!otherExists)
        {
            throw new NotFoundException(nameof(User), request.OtherUserId);
        }

        var existingId = await _dbContext.ConversationParticipants
            .AsNoTracking()
            .Where(p => p.UserId == userId)
            .Select(p => p.ConversationId)
            .Where(conversationId => _dbContext.ConversationParticipants.Any(o =>
                o.ConversationId == conversationId && o.UserId == request.OtherUserId))
            .FirstOrDefaultAsync(cancellationToken);

        Guid conversationId;
        if (existingId != Guid.Empty)
        {
            conversationId = existingId;
        }
        else
        {
            var conversation = new Conversation();
            conversation.Participants.Add(new ConversationParticipant { UserId = userId });
            conversation.Participants.Add(new ConversationParticipant { UserId = request.OtherUserId });
            _dbContext.Conversations.Add(conversation);
            await _dbContext.SaveChangesAsync(cancellationToken);
            conversationId = conversation.Id;
        }

        var other = await _dbContext.Users.AsNoTracking().FirstAsync(u => u.Id == request.OtherUserId, cancellationToken);
        var otherParticipant = await _dbContext.ConversationParticipants.AsNoTracking()
            .FirstAsync(p => p.ConversationId == conversationId && p.UserId == request.OtherUserId, cancellationToken);

        return Result.Success(new ConversationListItemResponse(
            conversationId,
            other.Id,
            other.FirstName,
            other.LastName,
            other.ProfileImageUrl,
            _presenceTracker.IsOnline(other.Id),
            otherParticipant.LastSeenAtUtc,
            null,
            null,
            0));
    }
}
