using FluentValidation;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Cases.Models;
using MediatR;

namespace LawConnect.Application.Features.Cases.Commands.SubmitCaseRating;

/// <summary>
/// Submits a 1-5 star rating (with optional review) for a case that has concluded. Contributes to
/// the same <c>Lawyer.RatingAverage</c>/<c>RatingCount</c> aggregate as appointment ratings — one
/// rating system, not two competing ones.
/// </summary>
public sealed record SubmitCaseRatingCommand(Guid CaseId, int Stars, string? ReviewText) : IRequest<Result<CaseRatingResponse>>;

/// <summary>
/// Validates <see cref="SubmitCaseRatingCommand"/>.
/// </summary>
public sealed class SubmitCaseRatingCommandValidator : AbstractValidator<SubmitCaseRatingCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SubmitCaseRatingCommandValidator"/> class.
    /// </summary>
    public SubmitCaseRatingCommandValidator()
    {
        RuleFor(x => x.CaseId).NotEmpty();
        RuleFor(x => x.Stars).InclusiveBetween(1, 5);
        RuleFor(x => x.ReviewText).MaximumLength(2000);
    }
}
