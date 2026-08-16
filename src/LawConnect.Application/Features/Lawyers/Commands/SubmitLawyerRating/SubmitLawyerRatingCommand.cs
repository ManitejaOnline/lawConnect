using FluentValidation;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Lawyers.Models;
using MediatR;

namespace LawConnect.Application.Features.Lawyers.Commands.SubmitLawyerRating;

/// <summary>
/// Submits a 1-5 star rating (with optional review text) for a completed engagement. One rating
/// per appointment — enforced both here and by a unique database index.
/// </summary>
public sealed record SubmitLawyerRatingCommand(
    Guid LawyerId,
    Guid AppointmentId,
    int Stars,
    string? ReviewText) : IRequest<Result<LawyerRatingSubmittedResponse>>;

/// <summary>
/// Validates <see cref="SubmitLawyerRatingCommand"/>.
/// </summary>
public sealed class SubmitLawyerRatingCommandValidator : AbstractValidator<SubmitLawyerRatingCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SubmitLawyerRatingCommandValidator"/> class.
    /// </summary>
    public SubmitLawyerRatingCommandValidator()
    {
        RuleFor(x => x.LawyerId).NotEmpty();
        RuleFor(x => x.AppointmentId).NotEmpty();
        RuleFor(x => x.Stars).InclusiveBetween(1, 5);
        RuleFor(x => x.ReviewText).MaximumLength(2000);
    }
}
