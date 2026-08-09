using FluentValidation;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Lawyers.Models;
using MediatR;

namespace LawConnect.Application.Features.Lawyers.Commands.UpdateLawyerProfile;

/// <summary>
/// Updates the current lawyer profile fields.
/// </summary>
public sealed record UpdateLawyerProfileCommand(
    string? Bio,
    string? BarCouncilNumber,
    int YearsOfExperience,
    bool IsAcceptingAppointments) : IRequest<Result<LawyerProfileResponse>>;

/// <summary>
/// Validates <see cref="UpdateLawyerProfileCommand"/>.
/// </summary>
public sealed class UpdateLawyerProfileCommandValidator : AbstractValidator<UpdateLawyerProfileCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateLawyerProfileCommandValidator"/> class.
    /// </summary>
    public UpdateLawyerProfileCommandValidator()
    {
        RuleFor(x => x.Bio).MaximumLength(4000).When(x => !string.IsNullOrWhiteSpace(x.Bio));
        RuleFor(x => x.BarCouncilNumber).MaximumLength(64).When(x => !string.IsNullOrWhiteSpace(x.BarCouncilNumber));
        RuleFor(x => x.YearsOfExperience).InclusiveBetween(0, 80);
    }
}
