using FluentValidation;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Cases.Models;
using LawConnect.Domain.Enums;
using MediatR;

namespace LawConnect.Application.Features.Cases.Commands.CreateCaseUpdate;

/// <summary>
/// Logs a case update. Lawyer-only — customers can read but never write case updates.
/// </summary>
public sealed record CreateCaseUpdateCommand(
    Guid CaseId,
    CaseUpdateType UpdateType,
    string Title,
    string Description) : IRequest<Result<CaseUpdateResponse>>;

/// <summary>
/// Validates <see cref="CreateCaseUpdateCommand"/>.
/// </summary>
public sealed class CreateCaseUpdateCommandValidator : AbstractValidator<CreateCaseUpdateCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CreateCaseUpdateCommandValidator"/> class.
    /// </summary>
    public CreateCaseUpdateCommandValidator()
    {
        RuleFor(x => x.CaseId).NotEmpty();
        RuleFor(x => x.UpdateType).IsInEnum();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(4000);
    }
}
