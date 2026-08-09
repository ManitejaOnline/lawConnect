using FluentValidation;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Lawyers.Models;
using MediatR;

namespace LawConnect.Application.Features.Lawyers.Commands.UpdateLawyerExperience;

/// <summary>
/// Updates an experience record for the current lawyer.
/// </summary>
public sealed record UpdateLawyerExperienceCommand(
    Guid ExperienceId,
    string OrganizationName,
    string Title,
    DateOnly? StartDate,
    DateOnly? EndDate,
    bool IsCurrent,
    string? Description) : IRequest<Result<LawyerExperienceResponse>>;

/// <summary>
/// Validates <see cref="UpdateLawyerExperienceCommand"/>.
/// </summary>
public sealed class UpdateLawyerExperienceCommandValidator : AbstractValidator<UpdateLawyerExperienceCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateLawyerExperienceCommandValidator"/> class.
    /// </summary>
    public UpdateLawyerExperienceCommandValidator()
    {
        RuleFor(x => x.ExperienceId).NotEmpty();
        RuleFor(x => x.OrganizationName).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Title).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Description).MaximumLength(2000).When(x => !string.IsNullOrWhiteSpace(x.Description));
        RuleFor(x => x).Must(x => !x.StartDate.HasValue || !x.EndDate.HasValue || x.EndDate >= x.StartDate)
            .WithMessage("End date must be on or after start date.");
    }
}
