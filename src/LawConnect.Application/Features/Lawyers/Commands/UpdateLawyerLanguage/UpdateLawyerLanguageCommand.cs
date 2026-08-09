using FluentValidation;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Lawyers.Models;
using MediatR;

namespace LawConnect.Application.Features.Lawyers.Commands.UpdateLawyerLanguage;

/// <summary>
/// Updates a language for the current lawyer.
/// </summary>
public sealed record UpdateLawyerLanguageCommand(
    Guid LanguageId,
    string LanguageName,
    string? Proficiency) : IRequest<Result<LawyerLanguageResponse>>;

/// <summary>
/// Validates <see cref="UpdateLawyerLanguageCommand"/>.
/// </summary>
public sealed class UpdateLawyerLanguageCommandValidator : AbstractValidator<UpdateLawyerLanguageCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateLawyerLanguageCommandValidator"/> class.
    /// </summary>
    public UpdateLawyerLanguageCommandValidator()
    {
        RuleFor(x => x.LanguageId).NotEmpty();
        RuleFor(x => x.LanguageName).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Proficiency).MaximumLength(32).When(x => !string.IsNullOrWhiteSpace(x.Proficiency));
    }
}
