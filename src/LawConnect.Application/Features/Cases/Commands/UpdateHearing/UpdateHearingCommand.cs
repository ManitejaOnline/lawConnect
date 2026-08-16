using FluentValidation;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Cases.Models;
using MediatR;

namespace LawConnect.Application.Features.Cases.Commands.UpdateHearing;

/// <summary>
/// Edits a hearing's scheduled fields. Only valid while the hearing is still Scheduled — once
/// completed/adjourned/cancelled, use RecordHearingOutcome instead.
/// </summary>
public sealed record UpdateHearingCommand(
    Guid CaseId,
    Guid HearingId,
    DateTime HearingDate,
    string? CourtName,
    string? HearingType,
    string? Purpose) : IRequest<Result<HearingResponse>>;

/// <summary>
/// Validates <see cref="UpdateHearingCommand"/>.
/// </summary>
public sealed class UpdateHearingCommandValidator : AbstractValidator<UpdateHearingCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateHearingCommandValidator"/> class.
    /// </summary>
    public UpdateHearingCommandValidator()
    {
        RuleFor(x => x.CaseId).NotEmpty();
        RuleFor(x => x.HearingId).NotEmpty();
        RuleFor(x => x.HearingDate).NotEqual(default(DateTime));
        RuleFor(x => x.CourtName).MaximumLength(256);
        RuleFor(x => x.HearingType).MaximumLength(128);
        RuleFor(x => x.Purpose).MaximumLength(1000);
    }
}
