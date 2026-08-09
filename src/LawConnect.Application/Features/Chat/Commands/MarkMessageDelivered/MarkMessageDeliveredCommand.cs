using FluentValidation;
using LawConnect.Application.Common.Models;
using MediatR;

namespace LawConnect.Application.Features.Chat.Commands.MarkMessageDelivered;

/// <summary>
/// Marks a message as delivered for the current user.
/// </summary>
public sealed record MarkMessageDeliveredCommand(Guid MessageId) : IRequest<Result>;

/// <summary>
/// Validates <see cref="MarkMessageDeliveredCommand"/>.
/// </summary>
public sealed class MarkMessageDeliveredCommandValidator : AbstractValidator<MarkMessageDeliveredCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MarkMessageDeliveredCommandValidator"/> class.
    /// </summary>
    public MarkMessageDeliveredCommandValidator() => RuleFor(x => x.MessageId).NotEmpty();
}
