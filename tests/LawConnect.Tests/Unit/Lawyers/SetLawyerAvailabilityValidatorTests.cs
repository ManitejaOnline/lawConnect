using FluentValidation.TestHelper;
using LawConnect.Application.Features.Lawyers.Commands.SetLawyerAvailability;
using LawConnect.Application.Features.Lawyers.Models;
using LawConnect.Domain.Enums;

namespace LawConnect.Tests.Unit.Lawyers;

public sealed class SetLawyerAvailabilityValidatorTests
{
    private readonly SetLawyerAvailabilityCommandValidator _validator = new();

    [Fact]
    public void OverlappingRanges_AreRejected()
    {
        var command = new SetLawyerAvailabilityCommand(
        [
            new AvailabilitySlotInput(DayOfWeekFlag.Monday, new TimeOnly(9, 0), new TimeOnly(13, 0), true, "Asia/Kolkata"),
            new AvailabilitySlotInput(DayOfWeekFlag.Monday, new TimeOnly(12, 0), new TimeOnly(18, 0), true, "Asia/Kolkata")
        ]);

        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Slots);
    }

    [Fact]
    public void AdjacentLunchBreakRanges_AreAccepted()
    {
        var command = new SetLawyerAvailabilityCommand(
        [
            new AvailabilitySlotInput(DayOfWeekFlag.Monday, new TimeOnly(9, 0), new TimeOnly(13, 0), true, "Asia/Kolkata"),
            new AvailabilitySlotInput(DayOfWeekFlag.Monday, new TimeOnly(14, 0), new TimeOnly(18, 0), true, "Asia/Kolkata")
        ]);

        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EndBeforeStart_IsRejected()
    {
        var command = new SetLawyerAvailabilityCommand(
        [
            new AvailabilitySlotInput(DayOfWeekFlag.Tuesday, new TimeOnly(18, 0), new TimeOnly(9, 0), true, "Asia/Kolkata")
        ]);

        var result = _validator.TestValidate(command);
        Assert.False(result.IsValid);
    }
}
