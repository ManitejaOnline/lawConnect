using FluentValidation;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Lawyers.Models;
using MediatR;

namespace LawConnect.Application.Features.Lawyers.Commands.SetLawyerAvailability;

/// <summary>
/// Replaces weekly availability for the current lawyer.
/// </summary>
public sealed record SetLawyerAvailabilityCommand(
    IReadOnlyList<AvailabilitySlotInput> Slots) : IRequest<Result<IReadOnlyList<LawyerAvailabilityResponse>>>;

/// <summary>
/// Validates <see cref="SetLawyerAvailabilityCommand"/>.
/// </summary>
public sealed class SetLawyerAvailabilityCommandValidator : AbstractValidator<SetLawyerAvailabilityCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SetLawyerAvailabilityCommandValidator"/> class.
    /// </summary>
    public SetLawyerAvailabilityCommandValidator()
    {
        RuleFor(x => x.Slots).NotNull();
        RuleForEach(x => x.Slots).ChildRules(slot =>
        {
            slot.RuleFor(s => s.DayOfWeek).IsInEnum();
            slot.RuleFor(s => s.TimeZoneId).NotEmpty().MaximumLength(64);
            slot.RuleFor(s => s).Must(s => s.EndTime > s.StartTime)
                .WithMessage("End time must be after start time.");
        });
        RuleFor(x => x.Slots)
            .Must(HaveNoOverlaps)
            .WithMessage("Availability ranges on the same day must not overlap.");
    }

    private static bool HaveNoOverlaps(IReadOnlyList<AvailabilitySlotInput> slots)
    {
        foreach (var day in slots.Where(s => s.IsAvailable).GroupBy(s => s.DayOfWeek))
        {
            var ordered = day.OrderBy(s => s.StartTime).ToList();
            for (var i = 1; i < ordered.Count; i++)
            {
                if (ordered[i].StartTime < ordered[i - 1].EndTime)
                {
                    return false;
                }
            }
        }

        return true;
    }
}
