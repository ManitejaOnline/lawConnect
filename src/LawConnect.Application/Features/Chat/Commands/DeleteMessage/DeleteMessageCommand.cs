using FluentValidation;
using LawConnect.Application.Common.Models;
using MediatR;

namespace LawConnect.Application.Features.Chat.Commands.DeleteMessage;

/// <summary>
/// Soft-deletes a message owned by the current user.
/// </summary>
public sealed record DeleteMessageCommand(Guid MessageId) : IRequest<Result>;

/// <summary>
/// Validates <see cref="DeleteMessageCommand"/>.
/// </summary>
public sealed class DeleteMessageCommandValidator : AbstractValidator<DeleteMessageCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteMessageCommandValidator"/> class.
    /// </summary>
    public DeleteMessageCommandValidator() => RuleFor(x => x.MessageId).NotEmpty();
}
