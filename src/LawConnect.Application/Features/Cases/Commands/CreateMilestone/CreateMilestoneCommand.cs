using FluentValidation;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Cases.Models;
using MediatR;

namespace LawConnect.Application.Features.Cases.Commands.CreateMilestone;

/// <summary>
/// Creates a milestone on a case.
/// </summary>
public sealed record CreateMilestoneCommand(
    Guid CaseId,
    string Title,
    string? Description,
    DateTime? DueDate) : IRequest<Result<CaseMilestoneResponse>>;

/// <summary>
/// Validates <see cref="CreateMilestoneCommand"/>.
/// </summary>
public sealed class CreateMilestoneCommandValidator : AbstractValidator<CreateMilestoneCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CreateMilestoneCommandValidator"/> class.
    /// </summary>
    public CreateMilestoneCommandValidator()
    {
        RuleFor(x => x.CaseId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Description).MaximumLength(2000);
    }
}
