using FluentValidation;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Lawyers.Models;
using MediatR;

namespace LawConnect.Application.Features.Lawyers.Commands.UpdateEmergencyAvailability;

/// <summary>
/// Updates emergency availability for the current lawyer.
/// </summary>
public sealed record UpdateEmergencyAvailabilityCommand(
    bool IsAvailableForEmergency,
    decimal? EmergencyFee) : IRequest<Result<LawyerProfileResponse>>;

/// <summary>
/// Validates <see cref="UpdateEmergencyAvailabilityCommand"/>.
/// </summary>
public sealed class UpdateEmergencyAvailabilityCommandValidator : AbstractValidator<UpdateEmergencyAvailabilityCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateEmergencyAvailabilityCommandValidator"/> class.
    /// </summary>
    public UpdateEmergencyAvailabilityCommandValidator()
    {
        RuleFor(x => x.EmergencyFee).GreaterThanOrEqualTo(0).When(x => x.EmergencyFee.HasValue);
        RuleFor(x => x.EmergencyFee)
            .NotNull()
            .When(x => x.IsAvailableForEmergency)
            .WithMessage("Emergency fee is required when emergency availability is enabled.");
    }
}
