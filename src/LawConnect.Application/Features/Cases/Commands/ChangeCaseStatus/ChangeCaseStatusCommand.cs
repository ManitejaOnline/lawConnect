using FluentValidation;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Cases.Models;
using LawConnect.Domain.Enums;
using MediatR;

namespace LawConnect.Application.Features.Cases.Commands.ChangeCaseStatus;

/// <summary>
/// Changes a case's status. Auto-sets/clears <see cref="Domain.Entities.Case.ClosedDate"/> and
/// logs a <see cref="CaseUpdateType.StatusChange"/> case update, which is what feeds the unified
/// timeline for status transitions.
/// </summary>
public sealed record ChangeCaseStatusCommand(Guid CaseId, CaseStatus Status, string? Note) : IRequest<Result<CaseResponse>>;

/// <summary>
/// Validates <see cref="ChangeCaseStatusCommand"/>.
/// </summary>
public sealed class ChangeCaseStatusCommandValidator : AbstractValidator<ChangeCaseStatusCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ChangeCaseStatusCommandValidator"/> class.
    /// </summary>
    public ChangeCaseStatusCommandValidator()
    {
        RuleFor(x => x.CaseId).NotEmpty();
        RuleFor(x => x.Status).IsInEnum();
        RuleFor(x => x.Note).MaximumLength(2000);
    }
}
