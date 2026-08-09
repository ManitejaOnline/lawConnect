using FluentValidation;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Lawyers.Models;
using MediatR;

namespace LawConnect.Application.Features.Lawyers.Commands.AddLawyerLanguage;

/// <summary>
/// Adds a language for the current lawyer.
/// </summary>
public sealed record AddLawyerLanguageCommand(
    string LanguageCode,
    string LanguageName,
    string? Proficiency) : IRequest<Result<LawyerLanguageResponse>>;

/// <summary>
/// Validates <see cref="AddLawyerLanguageCommand"/>.
/// </summary>
public sealed class AddLawyerLanguageCommandValidator : AbstractValidator<AddLawyerLanguageCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AddLawyerLanguageCommandValidator"/> class.
    /// </summary>
    public AddLawyerLanguageCommandValidator()
    {
        RuleFor(x => x.LanguageCode).NotEmpty().MaximumLength(16);
        RuleFor(x => x.LanguageName).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Proficiency).MaximumLength(32).When(x => !string.IsNullOrWhiteSpace(x.Proficiency));
    }
}
