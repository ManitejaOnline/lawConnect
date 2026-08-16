using FluentValidation;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Cases.Models;
using LawConnect.Domain.Enums;
using MediatR;

namespace LawConnect.Application.Features.Cases.Commands.UpdateCase;

/// <summary>
/// Updates a case's descriptive fields. Does not change <see cref="CaseStatus"/> — use
/// ChangeCaseStatus for that.
/// </summary>
public sealed record UpdateCaseCommand(
    Guid CaseId,
    Guid SpecializationId,
    string Title,
    string? CaseNumber,
    string? Description,
    string? CourtName,
    CasePriority Priority) : IRequest<Result<CaseResponse>>;

/// <summary>
/// Validates <see cref="UpdateCaseCommand"/>.
/// </summary>
public sealed class UpdateCaseCommandValidator : AbstractValidator<UpdateCaseCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateCaseCommandValidator"/> class.
    /// </summary>
    public UpdateCaseCommandValidator()
    {
        RuleFor(x => x.CaseId).NotEmpty();
        RuleFor(x => x.SpecializationId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(256);
        RuleFor(x => x.CaseNumber).MaximumLength(128);
        RuleFor(x => x.Description).MaximumLength(4000);
        RuleFor(x => x.CourtName).MaximumLength(256);
        RuleFor(x => x.Priority).IsInEnum();
    }
}
