using FluentValidation;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Cases.Models;
using MediatR;

namespace LawConnect.Application.Features.Cases.Commands.CreateHearing;

/// <summary>
/// Schedules a new hearing on a case.
/// </summary>
public sealed record CreateHearingCommand(
    Guid CaseId,
    DateTime HearingDate,
    string? CourtName,
    string? HearingType,
    string? Purpose) : IRequest<Result<HearingResponse>>;

/// <summary>
/// Validates <see cref="CreateHearingCommand"/>.
/// </summary>
public sealed class CreateHearingCommandValidator : AbstractValidator<CreateHearingCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CreateHearingCommandValidator"/> class.
    /// </summary>
    public CreateHearingCommandValidator()
    {
        RuleFor(x => x.CaseId).NotEmpty();
        RuleFor(x => x.HearingDate).NotEqual(default(DateTime));
        RuleFor(x => x.CourtName).MaximumLength(256);
        RuleFor(x => x.HearingType).MaximumLength(128);
        RuleFor(x => x.Purpose).MaximumLength(1000);
    }
}
