using FluentValidation;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Common.Validation;
using LawConnect.Application.Features.Lawyers.Models;
using MediatR;

namespace LawConnect.Application.Features.Lawyers.Commands.RegisterLawyer;

/// <summary>
/// Registers the current user as a lawyer.
/// </summary>
public sealed record RegisterLawyerCommand(
    string? BarCouncilNumber,
    string? Bio,
    int YearsOfExperience,
    decimal ConsultationFee,
    decimal? EmergencyFee,
    IReadOnlyList<Guid> SpecializationIds) : IRequest<Result<LawyerProfileResponse>>;

/// <summary>
/// Validates <see cref="RegisterLawyerCommand"/>.
/// </summary>
public sealed class RegisterLawyerCommandValidator : AbstractValidator<RegisterLawyerCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RegisterLawyerCommandValidator"/> class.
    /// </summary>
    public RegisterLawyerCommandValidator()
    {
        RuleFor(x => x.BarCouncilNumber).MaximumLength(64).When(x => !string.IsNullOrWhiteSpace(x.BarCouncilNumber));
        RuleFor(x => x.Bio).MaximumLength(4000).When(x => !string.IsNullOrWhiteSpace(x.Bio));
        RuleFor(x => x.YearsOfExperience).InclusiveBetween(0, 80);
        RuleFor(x => x.ConsultationFee).NonNegativeMoney();
        RuleFor(x => x.EmergencyFee)
            .GreaterThanOrEqualTo(0)
            .When(x => x.EmergencyFee.HasValue);
        RuleFor(x => x.SpecializationIds).NotNull().Must(ids => ids.Count > 0)
            .WithMessage("At least one practice area is required.");
        RuleForEach(x => x.SpecializationIds).NotEmpty();
    }
}
