using FluentValidation.TestHelper;
using LawConnect.Application.Features.Lawyers.Commands.RegisterLawyer;
using LawConnect.Application.Features.Lawyers.Commands.SetPracticeAreas;
using LawConnect.Application.Features.Lawyers.Models;
using LawConnect.Shared.Constants;

namespace LawConnect.Tests.Unit.Lawyers;

public sealed class LawyerValidatorTests
{
    [Fact]
    public void RegisterLawyer_EmptySpecializations_Fails()
    {
        var validator = new RegisterLawyerCommandValidator();
        var result = validator.TestValidate(new RegisterLawyerCommand(
            "BAR-1",
            "Bio",
            5,
            1000m,
            null,
            Array.Empty<Guid>()));

        result.ShouldHaveValidationErrorFor(x => x.SpecializationIds);
    }

    [Fact]
    public void SetPracticeAreas_RequiresExactlyOnePrimary()
    {
        var validator = new SetPracticeAreasCommandValidator();
        var result = validator.TestValidate(new SetPracticeAreasCommand(
        [
            new PracticeAreaInput(SeedIds.Specializations.CivilLaw, 5, false),
            new PracticeAreaInput(SeedIds.Specializations.FamilyLaw, 3, false)
        ]));

        result.ShouldHaveValidationErrorFor(x => x.PracticeAreas);
    }

    [Fact]
    public void SetPracticeAreas_Valid_Passes()
    {
        var validator = new SetPracticeAreasCommandValidator();
        var result = validator.TestValidate(new SetPracticeAreasCommand(
        [
            new PracticeAreaInput(SeedIds.Specializations.CivilLaw, 5, true),
            new PracticeAreaInput(SeedIds.Specializations.FamilyLaw, 3, false)
        ]));

        result.ShouldNotHaveAnyValidationErrors();
    }
}
