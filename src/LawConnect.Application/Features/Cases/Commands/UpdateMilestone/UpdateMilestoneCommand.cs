using FluentValidation;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Cases.Models;
using LawConnect.Domain.Enums;
using MediatR;

namespace LawConnect.Application.Features.Cases.Commands.UpdateMilestone;

/// <summary>
/// Edits a milestone's fields, including moving it between Pending/InProgress/Cancelled. Does not
/// accept <see cref="MilestoneStatus.Completed"/> — use CompleteMilestone for that, and never
/// accepts <see cref="MilestoneStatus.Overdue"/> — only the overdue sweep sets that.
/// </summary>
public sealed record UpdateMilestoneCommand(
    Guid CaseId,
    Guid MilestoneId,
    string Title,
    string? Description,
    DateTime? DueDate,
    MilestoneStatus Status) : IRequest<Result<CaseMilestoneResponse>>;

/// <summary>
/// Validates <see cref="UpdateMilestoneCommand"/>.
/// </summary>
public sealed class UpdateMilestoneCommandValidator : AbstractValidator<UpdateMilestoneCommand>
{
    private static readonly MilestoneStatus[] AllowedStatuses =
    [
        MilestoneStatus.Pending, MilestoneStatus.InProgress, MilestoneStatus.Cancelled
    ];

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateMilestoneCommandValidator"/> class.
    /// </summary>
    public UpdateMilestoneCommandValidator()
    {
        RuleFor(x => x.CaseId).NotEmpty();
        RuleFor(x => x.MilestoneId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.Status).Must(s => AllowedStatuses.Contains(s))
            .WithMessage("Status must be Pending, InProgress, or Cancelled — use complete/the overdue sweep for the others.");
    }
}
