using FluentValidation;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Chat.Models;
using MediatR;

namespace LawConnect.Application.Features.Chat.Commands.StartConversation;

/// <summary>
/// Starts or returns an existing one-to-one conversation.
/// </summary>
public sealed record StartConversationCommand(Guid OtherUserId) : IRequest<Result<ConversationListItemResponse>>;

/// <summary>
/// Validates <see cref="StartConversationCommand"/>.
/// </summary>
public sealed class StartConversationCommandValidator : AbstractValidator<StartConversationCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StartConversationCommandValidator"/> class.
    /// </summary>
    public StartConversationCommandValidator() => RuleFor(x => x.OtherUserId).NotEmpty();
}
