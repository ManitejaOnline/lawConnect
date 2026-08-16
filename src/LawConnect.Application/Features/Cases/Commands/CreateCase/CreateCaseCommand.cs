using FluentValidation;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Cases.Models;
using LawConnect.Domain.Enums;
using MediatR;

namespace LawConnect.Application.Features.Cases.Commands.CreateCase;

/// <summary>
/// Creates a new case for a customer the current lawyer has an established appointment
/// relationship with.
/// </summary>
public sealed record CreateCaseCommand(
    Guid CustomerId,
    Guid SpecializationId,
    string Title,
    string? CaseNumber,
    string? Description,
    string? CourtName,
    CasePriority Priority,
    DateTime StartDate,
    Guid? SourceAppointmentId) : IRequest<Result<CaseResponse>>;

/// <summary>
/// Validates <see cref="CreateCaseCommand"/>.
/// </summary>
public sealed class CreateCaseCommandValidator : AbstractValidator<CreateCaseCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CreateCaseCommandValidator"/> class.
    /// </summary>
    public CreateCaseCommandValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.SpecializationId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(256);
        RuleFor(x => x.CaseNumber).MaximumLength(128);
        RuleFor(x => x.Description).MaximumLength(4000);
        RuleFor(x => x.CourtName).MaximumLength(256);
        RuleFor(x => x.Priority).IsInEnum();
        RuleFor(x => x.StartDate).NotEqual(default(DateTime));
    }
}
