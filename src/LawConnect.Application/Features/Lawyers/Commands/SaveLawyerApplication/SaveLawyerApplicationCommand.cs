using FluentValidation;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Lawyers.Models;
using MediatR;

namespace LawConnect.Application.Features.Lawyers.Commands.SaveLawyerApplication;

/// <summary>
/// Saves lawyer application draft fields (professional + consultation profile).
/// </summary>
public sealed record SaveLawyerApplicationCommand(
    string? StateBarCouncil,
    string? BarCouncilNumber,
    IReadOnlyList<Guid>? SpecializationIds,
    int? YearsOfExperience,
    string? Languages,
    string? Education,
    decimal? ConsultationFee,
    string? CityName,
    string? StateName,
    string? CountryName,
    string? Bio,
    IReadOnlyList<AvailabilitySlotInput>? Availability) : IRequest<Result<LawyerProfileResponse>>;

/// <summary>
/// Validates <see cref="SaveLawyerApplicationCommand"/>.
/// </summary>
public sealed class SaveLawyerApplicationCommandValidator : AbstractValidator<SaveLawyerApplicationCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SaveLawyerApplicationCommandValidator"/> class.
    /// </summary>
    public SaveLawyerApplicationCommandValidator()
    {
        RuleFor(x => x.StateBarCouncil).MaximumLength(128).When(x => !string.IsNullOrWhiteSpace(x.StateBarCouncil));
        RuleFor(x => x.BarCouncilNumber).MaximumLength(64).When(x => !string.IsNullOrWhiteSpace(x.BarCouncilNumber));
        RuleFor(x => x.YearsOfExperience).InclusiveBetween(0, 80).When(x => x.YearsOfExperience.HasValue);
        RuleFor(x => x.ConsultationFee)
            .GreaterThanOrEqualTo(0)
            .When(x => x.ConsultationFee.HasValue);
        RuleFor(x => x.CityName).MaximumLength(128).When(x => !string.IsNullOrWhiteSpace(x.CityName));
        RuleFor(x => x.StateName).MaximumLength(128).When(x => !string.IsNullOrWhiteSpace(x.StateName));
        RuleFor(x => x.CountryName).MaximumLength(128).When(x => !string.IsNullOrWhiteSpace(x.CountryName));
        RuleFor(x => x.Bio).MaximumLength(4000).When(x => !string.IsNullOrWhiteSpace(x.Bio));
        RuleFor(x => x.Languages).MaximumLength(500).When(x => !string.IsNullOrWhiteSpace(x.Languages));
        RuleFor(x => x.Education).MaximumLength(500).When(x => !string.IsNullOrWhiteSpace(x.Education));
        RuleForEach(x => x.SpecializationIds).NotEmpty().When(x => x.SpecializationIds is not null);
    }
}
