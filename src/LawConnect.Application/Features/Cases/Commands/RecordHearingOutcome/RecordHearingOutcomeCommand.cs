using FluentValidation;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Cases.Models;
using LawConnect.Domain.Enums;
using MediatR;

namespace LawConnect.Application.Features.Cases.Commands.RecordHearingOutcome;

/// <summary>
/// Records the outcome of a hearing that has taken place (or was adjourned/cancelled). A genuinely
/// new next hearing is scheduled as its own new <see cref="Domain.Entities.Hearing"/> via
/// CreateHearing — <see cref="NextAction"/> here is free-text guidance, not itself a date.
/// </summary>
public sealed record RecordHearingOutcomeCommand(
    Guid CaseId,
    Guid HearingId,
    HearingStatus Status,
    string? Outcome,
    string? Notes,
    string? NextAction) : IRequest<Result<HearingResponse>>;

/// <summary>
/// Validates <see cref="RecordHearingOutcomeCommand"/>.
/// </summary>
public sealed class RecordHearingOutcomeCommandValidator : AbstractValidator<RecordHearingOutcomeCommand>
{
    private static readonly HearingStatus[] AllowedStatuses =
    [
        HearingStatus.Completed, HearingStatus.Adjourned, HearingStatus.Cancelled
    ];

    /// <summary>
    /// Initializes a new instance of the <see cref="RecordHearingOutcomeCommandValidator"/> class.
    /// </summary>
    public RecordHearingOutcomeCommandValidator()
    {
        RuleFor(x => x.CaseId).NotEmpty();
        RuleFor(x => x.HearingId).NotEmpty();
        RuleFor(x => x.Status).Must(s => AllowedStatuses.Contains(s))
            .WithMessage("Status must be Completed, Adjourned, or Cancelled.");
        RuleFor(x => x.Outcome).MaximumLength(2000);
        RuleFor(x => x.Notes).MaximumLength(2000);
        RuleFor(x => x.NextAction).MaximumLength(500);
    }
}
