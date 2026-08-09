using FluentValidation;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Lawyers.Models;
using MediatR;

namespace LawConnect.Application.Features.Lawyers.Commands.AddLawyerEducation;

/// <summary>
/// Adds an education record for the current lawyer.
/// </summary>
public sealed record AddLawyerEducationCommand(
    string InstitutionName,
    string Degree,
    string? FieldOfStudy,
    int? StartYear,
    int? EndYear,
    string? Description) : IRequest<Result<LawyerEducationResponse>>;

/// <summary>
/// Validates <see cref="AddLawyerEducationCommand"/>.
/// </summary>
public sealed class AddLawyerEducationCommandValidator : AbstractValidator<AddLawyerEducationCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AddLawyerEducationCommandValidator"/> class.
    /// </summary>
    public AddLawyerEducationCommandValidator()
    {
        RuleFor(x => x.InstitutionName).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Degree).NotEmpty().MaximumLength(128);
        RuleFor(x => x.FieldOfStudy).MaximumLength(128).When(x => !string.IsNullOrWhiteSpace(x.FieldOfStudy));
        RuleFor(x => x.Description).MaximumLength(2000).When(x => !string.IsNullOrWhiteSpace(x.Description));
        RuleFor(x => x.StartYear).InclusiveBetween(1950, 2100).When(x => x.StartYear.HasValue);
        RuleFor(x => x.EndYear).InclusiveBetween(1950, 2100).When(x => x.EndYear.HasValue);
        RuleFor(x => x).Must(x => !x.StartYear.HasValue || !x.EndYear.HasValue || x.EndYear >= x.StartYear)
            .WithMessage("End year must be greater than or equal to start year.");
    }
}
