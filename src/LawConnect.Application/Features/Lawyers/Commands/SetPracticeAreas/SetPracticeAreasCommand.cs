using FluentValidation;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Lawyers.Models;
using MediatR;

namespace LawConnect.Application.Features.Lawyers.Commands.SetPracticeAreas;

/// <summary>
/// Replaces practice areas for the current lawyer.
/// </summary>
public sealed record SetPracticeAreasCommand(
    IReadOnlyList<PracticeAreaInput> PracticeAreas) : IRequest<Result<LawyerProfileResponse>>;

/// <summary>
/// Validates <see cref="SetPracticeAreasCommand"/>.
/// </summary>
public sealed class SetPracticeAreasCommandValidator : AbstractValidator<SetPracticeAreasCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SetPracticeAreasCommandValidator"/> class.
    /// </summary>
    public SetPracticeAreasCommandValidator()
    {
        RuleFor(x => x.PracticeAreas).NotNull().Must(x => x.Count > 0)
            .WithMessage("At least one practice area is required.");
        RuleForEach(x => x.PracticeAreas).ChildRules(area =>
        {
            area.RuleFor(a => a.SpecializationId).NotEmpty();
            area.RuleFor(a => a.YearsOfExperience).InclusiveBetween(0, 80);
        });
        RuleFor(x => x.PracticeAreas)
            .Must(areas => areas.Count(a => a.IsPrimary) == 1)
            .WithMessage("Exactly one practice area must be marked primary.");
        RuleFor(x => x.PracticeAreas)
            .Must(areas => areas.Select(a => a.SpecializationId).Distinct().Count() == areas.Count)
            .WithMessage("Duplicate specializations are not allowed.");
    }
}
