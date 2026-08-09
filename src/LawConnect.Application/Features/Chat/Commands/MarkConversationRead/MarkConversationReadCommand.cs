using FluentValidation;
using LawConnect.Application.Common.Models;
using MediatR;

namespace LawConnect.Application.Features.Chat.Commands.MarkConversationRead;

/// <summary>
/// Marks messages in a conversation as read up to a message id.
/// </summary>
public sealed record MarkConversationReadCommand(Guid ConversationId, Guid MessageId) : IRequest<Result>;

/// <summary>
/// Validates <see cref="MarkConversationReadCommand"/>.
/// </summary>
public sealed class MarkConversationReadCommandValidator : AbstractValidator<MarkConversationReadCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MarkConversationReadCommandValidator"/> class.
    /// </summary>
    public MarkConversationReadCommandValidator()
    {
        RuleFor(x => x.ConversationId).NotEmpty();
        RuleFor(x => x.MessageId).NotEmpty();
    }
}
