using FluentValidation;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Common.Validation;
using LawConnect.Application.Features.Users.Models;
using MediatR;

namespace LawConnect.Application.Features.Users.Commands.UploadProfilePicture;

/// <summary>
/// Uploads a profile picture for the current user.
/// </summary>
public sealed record UploadProfilePictureCommand(
    Stream Content,
    string FileName,
    string ContentType,
    long FileSizeBytes) : IRequest<Result<UserProfileResponse>>;

/// <summary>
/// Validates <see cref="UploadProfilePictureCommand"/>.
/// </summary>
public sealed class UploadProfilePictureCommandValidator : AbstractValidator<UploadProfilePictureCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UploadProfilePictureCommandValidator"/> class.
    /// </summary>
    public UploadProfilePictureCommandValidator()
    {
        RuleFor(x => x.Content).NotNull();
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(256);
        RuleFor(x => x.ContentType).ImageContentType();
        RuleFor(x => x.FileSizeBytes).GreaterThan(0);
    }
}
