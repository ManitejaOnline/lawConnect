using FluentValidation;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Common.Validation;
using LawConnect.Application.Features.Users.Models;
using MediatR;

namespace LawConnect.Application.Features.Users.Commands.UpdateMyProfile;

/// <summary>
/// Updates the authenticated user's profile.
/// </summary>
public sealed record UpdateMyProfileCommand(
    string FirstName,
    string LastName,
    string? Email,
    DateOnly? DateOfBirth,
    string? Gender,
    string? PreferredLanguage) : IRequest<Result<UserProfileResponse>>;

/// <summary>
/// Validates <see cref="UpdateMyProfileCommand"/>.
/// </summary>
public sealed class UpdateMyProfileCommandValidator : AbstractValidator<UpdateMyProfileCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateMyProfileCommandValidator"/> class.
    /// </summary>
    public UpdateMyProfileCommandValidator()
    {
        RuleFor(x => x.FirstName).PersonName();
        RuleFor(x => x.LastName).PersonName();
        RuleFor(x => x.Email)
            .MaximumLength(256)
            .EmailAddress()
            .When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Gender).MaximumLength(32).When(x => !string.IsNullOrWhiteSpace(x.Gender));
        RuleFor(x => x.PreferredLanguage).MaximumLength(16).When(x => !string.IsNullOrWhiteSpace(x.PreferredLanguage));
        RuleFor(x => x.DateOfBirth)
            .LessThan(DateOnly.FromDateTime(DateTime.UtcNow))
            .When(x => x.DateOfBirth.HasValue);
    }
}
