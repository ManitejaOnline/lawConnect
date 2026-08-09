using FluentValidation;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Admin.Models;
using LawConnect.Domain.Enums;
using MediatR;

namespace LawConnect.Application.Features.Admin.Commands.ReviewLawyerApplication;

/// <summary>
/// Admin review action for a lawyer application.
/// </summary>
public enum LawyerReviewAction
{
    /// <summary>Approve the lawyer for operational access.</summary>
    Approve = 0,

    /// <summary>Reject the application.</summary>
    Reject = 1,

    /// <summary>Return the application to the lawyer for changes.</summary>
    RequestChanges = 2
}

/// <summary>
/// Approves, rejects, or requests changes on a lawyer application.
/// </summary>
public sealed record ReviewLawyerApplicationCommand(
    Guid LawyerId,
    LawyerReviewAction Action,
    string? Reason) : IRequest<Result<AdminLawyerApplicationDetail>>;

/// <summary>
/// Validates <see cref="ReviewLawyerApplicationCommand"/>.
/// </summary>
public sealed class ReviewLawyerApplicationCommandValidator : AbstractValidator<ReviewLawyerApplicationCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ReviewLawyerApplicationCommandValidator"/> class.
    /// </summary>
    public ReviewLawyerApplicationCommandValidator()
    {
        RuleFor(x => x.LawyerId).NotEmpty();
        RuleFor(x => x.Action).IsInEnum();
        RuleFor(x => x.Reason)
            .NotEmpty()
            .MaximumLength(2000)
            .When(x => x.Action is LawyerReviewAction.Reject or LawyerReviewAction.RequestChanges);
    }
}
