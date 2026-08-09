using LawConnect.Application.Common.Interfaces;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Common.Realtime;
using LawConnect.Application.Features.Chat.Services;
using LawConnect.Domain.Entities;
using LawConnect.Domain.Enums;
using LawConnect.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawConnect.Application.Features.Chat.Commands.MarkConversationRead;

/// <summary>
/// Handles <see cref="MarkConversationReadCommand"/>.
/// </summary>
public sealed class MarkConversationReadCommandHandler : IRequestHandler<MarkConversationReadCommand, Result>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IConversationAccessService _accessService;
    private readonly IChatRealtimePublisher _realtimePublisher;

    /// <summary>
    /// Initializes a new instance of the <see cref="MarkConversationReadCommandHandler"/> class.
    /// </summary>
    public MarkConversationReadCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider,
        IConversationAccessService accessService,
        IChatRealtimePublisher realtimePublisher)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
        _accessService = accessService;
        _realtimePublisher = realtimePublisher;
    }

    /// <inheritdoc />
    public async Task<Result> Handle(MarkConversationReadCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetRequiredUserId();
        await _accessService.EnsureParticipantAsync(request.ConversationId, userId, cancellationToken);

        var message = await _dbContext.Messages
            .AsNoTracking()
            .FirstOrDefaultAsync(
                m => m.Id == request.MessageId && m.ConversationId == request.ConversationId,
                cancellationToken)
            ?? throw new NotFoundException(nameof(Message), request.MessageId);

        var now = _dateTimeProvider.UtcNow;
        var statuses = await _dbContext.MessageReadStatuses
            .Where(s =>
                s.UserId == userId
                && s.Message.ConversationId == request.ConversationId
                && s.Message.SentAtUtc <= message.SentAtUtc
                && s.State != MessageDeliveryState.Read)
            .ToListAsync(cancellationToken);

        foreach (var status in statuses)
        {
            status.State = MessageDeliveryState.Read;
            status.ReadAtUtc = now;
            status.DeliveredAtUtc ??= now;
        }

        var participant = await _dbContext.ConversationParticipants
            .FirstAsync(p => p.ConversationId == request.ConversationId && p.UserId == userId, cancellationToken);
        participant.LastReadAtUtc = now;

        await _dbContext.SaveChangesAsync(cancellationToken);
        await _realtimePublisher.PublishReadReceiptAsync(
            request.ConversationId,
            request.MessageId,
            userId,
            now,
            cancellationToken);

        return Result.Success();
    }
}
