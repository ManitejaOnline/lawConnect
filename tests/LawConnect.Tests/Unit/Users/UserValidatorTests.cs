using FluentValidation.TestHelper;
using LawConnect.Application.Features.Users.Commands.UpdateMyProfile;
using LawConnect.Application.Features.Users.Commands.CreateAddress;

namespace LawConnect.Tests.Unit.Users;

public sealed class UserValidatorTests
{
    [Fact]
    public void UpdateMyProfile_InvalidName_Fails()
    {
        var validator = new UpdateMyProfileCommandValidator();
        var result = validator.TestValidate(new UpdateMyProfileCommand(
            "Asha123",
            "Patel",
            null,
            null,
            null,
            null));

        result.ShouldHaveValidationErrorFor(x => x.FirstName);
    }

    [Fact]
    public void CreateAddress_EmptyLine1_Fails()
    {
        var validator = new CreateAddressCommandValidator();
        var result = validator.TestValidate(new CreateAddressCommand(
            string.Empty,
            null,
            null,
            null,
            null,
            null,
            null,
            false,
            null));

        result.ShouldHaveValidationErrorFor(x => x.Line1);
    }
}
