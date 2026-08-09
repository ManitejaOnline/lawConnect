using LawConnect.Application.Common.Interfaces;
using LawConnect.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace LawConnect.Application.Features.Chat.Services;

/// <summary>
/// Shared conversation access checks to avoid duplicated authorization logic.
/// </summary>
public interface IConversationAccessService
{
    /// <summary>
    /// Ensures the current user participates in the conversation.
    /// </summary>
    Task EnsureParticipantAsync(Guid conversationId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the other participant user id in a one-to-one conversation.
    /// </summary>
    Task<Guid> GetOtherParticipantUserIdAsync(
        Guid conversationId,
        Guid userId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Default <see cref="IConversationAccessService"/> implementation.
/// </summary>
public sealed class ConversationAccessService : IConversationAccessService
{
    private readonly IApplicationDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConversationAccessService"/> class.
    /// </summary>
    public ConversationAccessService(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public async Task EnsureParticipantAsync(
        Guid conversationId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var isParticipant = await _dbContext.ConversationParticipants
            .AsNoTracking()
            .AnyAsync(p => p.ConversationId == conversationId && p.UserId == userId, cancellationToken);

        if (!isParticipant)
        {
            throw new ForbiddenException("You are not a participant in this conversation.");
        }
    }

    /// <inheritdoc />
    public async Task<Guid> GetOtherParticipantUserIdAsync(
        Guid conversationId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var other = await _dbContext.ConversationParticipants
            .AsNoTracking()
            .Where(p => p.ConversationId == conversationId && p.UserId != userId)
            .Select(p => p.UserId)
            .FirstOrDefaultAsync(cancellationToken);

        if (other == Guid.Empty)
        {
            throw new NotFoundException("Conversation participant was not found.");
        }

        return other;
    }
}
