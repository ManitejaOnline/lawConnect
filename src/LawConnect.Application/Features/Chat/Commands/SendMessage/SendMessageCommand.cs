using FluentValidation;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Chat.Models;
using LawConnect.Domain.Enums;
using MediatR;

namespace LawConnect.Application.Features.Chat.Commands.SendMessage;

/// <summary>
/// Sends a chat message.
/// </summary>
public sealed record SendMessageCommand(
    Guid ConversationId,
    string? Body,
    MessageType Type,
    string? AttachmentStorageKey,
    string? AttachmentFileName,
    string? AttachmentContentType,
    long? AttachmentFileSizeBytes) : IRequest<Result<MessageResponse>>;

/// <summary>
/// Validates <see cref="SendMessageCommand"/>.
/// </summary>
public sealed class SendMessageCommandValidator : AbstractValidator<SendMessageCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SendMessageCommandValidator"/> class.
    /// </summary>
    public SendMessageCommandValidator()
    {
        RuleFor(x => x.ConversationId).NotEmpty();
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.Body).MaximumLength(4000).When(x => !string.IsNullOrWhiteSpace(x.Body));
        RuleFor(x => x).Must(x =>
                x.Type != MessageType.Text
                || !string.IsNullOrWhiteSpace(x.Body))
            .WithMessage("Text messages require a body.");
        RuleFor(x => x).Must(x =>
                x.Type == MessageType.Text
                || (!string.IsNullOrWhiteSpace(x.AttachmentStorageKey)
                    && !string.IsNullOrWhiteSpace(x.AttachmentFileName)
                    && !string.IsNullOrWhiteSpace(x.AttachmentContentType)
                    && x.AttachmentFileSizeBytes > 0))
            .WithMessage("Attachment metadata is required for image/PDF messages.");
        RuleFor(x => x)
            .Must(BeIssuedChatUploadKey)
            .When(x => x.Type != MessageType.Text && !string.IsNullOrWhiteSpace(x.AttachmentStorageKey))
            .WithMessage("Attachment must reference a chat upload for this conversation.");
    }

    private static bool BeIssuedChatUploadKey(SendMessageCommand command)
    {
        var key = command.AttachmentStorageKey!.Trim().Replace('\\', '/');
        var prefix = $"chat/{command.ConversationId:N}/";
        return key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            && key.Length <= 256
            && !key.Split('/').Any(segment => segment is "" or "." or "..");
    }
}
