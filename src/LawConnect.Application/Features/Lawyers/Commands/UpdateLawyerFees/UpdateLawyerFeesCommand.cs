using FluentValidation;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Common.Validation;
using LawConnect.Application.Features.Lawyers.Models;
using MediatR;

namespace LawConnect.Application.Features.Lawyers.Commands.UpdateLawyerFees;

/// <summary>
/// Updates lawyer consultation and emergency fees.
/// </summary>
public sealed record UpdateLawyerFeesCommand(
    decimal ConsultationFee,
    decimal? EmergencyFee) : IRequest<Result<LawyerProfileResponse>>;

/// <summary>
/// Validates <see cref="UpdateLawyerFeesCommand"/>.
/// </summary>
public sealed class UpdateLawyerFeesCommandValidator : AbstractValidator<UpdateLawyerFeesCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateLawyerFeesCommandValidator"/> class.
    /// </summary>
    public UpdateLawyerFeesCommandValidator()
    {
        RuleFor(x => x.ConsultationFee).NonNegativeMoney();
        RuleFor(x => x.EmergencyFee).GreaterThanOrEqualTo(0).When(x => x.EmergencyFee.HasValue);
    }
}
