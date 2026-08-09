using FluentValidation;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Common.Validation;
using LawConnect.Application.Features.Chat.Models;
using MediatR;

namespace LawConnect.Application.Features.Chat.Commands.CreateChatUploadUrl;

/// <summary>
/// Creates a signed upload URL for a chat attachment.
/// </summary>
public sealed record CreateChatUploadUrlCommand(
    string FileName,
    string ContentType,
    long FileSizeBytes) : IRequest<Result<ChatUploadUrlResponse>>;

/// <summary>
/// Validates <see cref="CreateChatUploadUrlCommand"/>.
/// </summary>
public sealed class CreateChatUploadUrlCommandValidator : AbstractValidator<CreateChatUploadUrlCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CreateChatUploadUrlCommandValidator"/> class.
    /// </summary>
    public CreateChatUploadUrlCommandValidator()
    {
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(256);
        RuleFor(x => x.ContentType).NotEmpty().MaximumLength(128);
        RuleFor(x => x.FileSizeBytes).GreaterThan(0);
        RuleFor(x => x.ContentType)
            .Must(ct => ValidationRules.AllowedImageContentTypes.Contains(ct)
                        || string.Equals(ct, "application/pdf", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Only JPEG, PNG, WebP, or PDF attachments are allowed.");
    }
}
