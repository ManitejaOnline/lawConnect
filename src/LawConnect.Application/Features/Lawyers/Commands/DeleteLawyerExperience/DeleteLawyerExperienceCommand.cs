using FluentValidation;
using LawConnect.Application.Common.Models;
using MediatR;

namespace LawConnect.Application.Features.Lawyers.Commands.DeleteLawyerExperience;

/// <summary>
/// Deletes an experience record for the current lawyer.
/// </summary>
public sealed record DeleteLawyerExperienceCommand(Guid ExperienceId) : IRequest<Result>;

/// <summary>
/// Validates <see cref="DeleteLawyerExperienceCommand"/>.
/// </summary>
public sealed class DeleteLawyerExperienceCommandValidator : AbstractValidator<DeleteLawyerExperienceCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteLawyerExperienceCommandValidator"/> class.
    /// </summary>
    public DeleteLawyerExperienceCommandValidator() => RuleFor(x => x.ExperienceId).NotEmpty();
}
