using LawConnect.Application.Common.Interfaces;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Chat.Services;
using LawConnect.Domain.Entities;
using LawConnect.Domain.Enums;
using LawConnect.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawConnect.Application.Features.Chat.Commands.MarkMessageDelivered;

/// <summary>
/// Handles <see cref="MarkMessageDeliveredCommand"/>.
/// </summary>
public sealed class MarkMessageDeliveredCommandHandler : IRequestHandler<MarkMessageDeliveredCommand, Result>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IConversationAccessService _accessService;

    /// <summary>
    /// Initializes a new instance of the <see cref="MarkMessageDeliveredCommandHandler"/> class.
    /// </summary>
    public MarkMessageDeliveredCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider,
        IConversationAccessService accessService)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
        _accessService = accessService;
    }

    /// <inheritdoc />
    public async Task<Result> Handle(MarkMessageDeliveredCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetRequiredUserId();
        var message = await _dbContext.Messages
            .FirstOrDefaultAsync(m => m.Id == request.MessageId, cancellationToken)
            ?? throw new NotFoundException(nameof(Message), request.MessageId);

        await _accessService.EnsureParticipantAsync(message.ConversationId, userId, cancellationToken);
        if (message.SenderUserId == userId)
        {
            return Result.Success();
        }

        var status = await _dbContext.MessageReadStatuses
            .FirstOrDefaultAsync(s => s.MessageId == message.Id && s.UserId == userId, cancellationToken);

        if (status is null)
        {
            status = new MessageReadStatus { MessageId = message.Id, UserId = userId };
            _dbContext.MessageReadStatuses.Add(status);
        }

        if (status.State == MessageDeliveryState.Sent)
        {
            status.State = MessageDeliveryState.Delivered;
            status.DeliveredAtUtc = _dateTimeProvider.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return Result.Success();
    }
}
