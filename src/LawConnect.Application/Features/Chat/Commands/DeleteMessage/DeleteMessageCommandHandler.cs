using LawConnect.Application.Common.Interfaces;
using LawConnect.Application.Common.Models;
using LawConnect.Domain.Entities;
using LawConnect.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawConnect.Application.Features.Chat.Commands.DeleteMessage;

/// <summary>
/// Handles <see cref="DeleteMessageCommand"/>.
/// </summary>
public sealed class DeleteMessageCommandHandler : IRequestHandler<DeleteMessageCommand, Result>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteMessageCommandHandler"/> class.
    /// </summary>
    public DeleteMessageCommandHandler(IApplicationDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    /// <inheritdoc />
    public async Task<Result> Handle(DeleteMessageCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetRequiredUserId();
        var message = await _dbContext.Messages
            .FirstOrDefaultAsync(m => m.Id == request.MessageId, cancellationToken)
            ?? throw new NotFoundException(nameof(Message), request.MessageId);

        if (message.SenderUserId != userId)
        {
            throw new ForbiddenException("Only the sender can delete this message.");
        }

        _dbContext.Messages.Remove(message);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
