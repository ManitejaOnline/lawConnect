using FluentValidation;
using LawConnect.Application.Common.Models;
using MediatR;

namespace LawConnect.Application.Features.Lawyers.Commands.DeleteLawyerLanguage;

/// <summary>
/// Deletes a language for the current lawyer.
/// </summary>
public sealed record DeleteLawyerLanguageCommand(Guid LanguageId) : IRequest<Result>;

/// <summary>
/// Validates <see cref="DeleteLawyerLanguageCommand"/>.
/// </summary>
public sealed class DeleteLawyerLanguageCommandValidator : AbstractValidator<DeleteLawyerLanguageCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteLawyerLanguageCommandValidator"/> class.
    /// </summary>
    public DeleteLawyerLanguageCommandValidator() => RuleFor(x => x.LanguageId).NotEmpty();
}
