using FluentValidation;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Cases.Models;
using MediatR;

namespace LawConnect.Application.Features.Cases.Commands.CompleteMilestone;

/// <summary>
/// Marks a milestone as completed.
/// </summary>
public sealed record CompleteMilestoneCommand(Guid CaseId, Guid MilestoneId) : IRequest<Result<CaseMilestoneResponse>>;

/// <summary>
/// Validates <see cref="CompleteMilestoneCommand"/>.
/// </summary>
public sealed class CompleteMilestoneCommandValidator : AbstractValidator<CompleteMilestoneCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CompleteMilestoneCommandValidator"/> class.
    /// </summary>
    public CompleteMilestoneCommandValidator()
    {
        RuleFor(x => x.CaseId).NotEmpty();
        RuleFor(x => x.MilestoneId).NotEmpty();
    }
}
