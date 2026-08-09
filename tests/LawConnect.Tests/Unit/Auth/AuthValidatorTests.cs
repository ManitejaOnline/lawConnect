using FluentValidation.TestHelper;
using LawConnect.Application.Features.Auth.Commands.RequestOtp;
using LawConnect.Application.Features.Auth.Commands.VerifyOtp;
using LawConnect.Domain.Enums;

namespace LawConnect.Tests.Unit.Auth;

public sealed class AuthValidatorTests
{
    [Fact]
    public void RequestOtp_InvalidPhone_Fails()
    {
        var validator = new RequestOtpCommandValidator();
        var result = validator.TestValidate(new RequestOtpCommand("9876543210"));
        result.ShouldHaveValidationErrorFor(x => x.PhoneNumber);
    }

    [Fact]
    public void RequestOtp_ValidE164_Passes()
    {
        var validator = new RequestOtpCommandValidator();
        var result = validator.TestValidate(new RequestOtpCommand("+919876543210"));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void VerifyOtp_NonNumericCode_Fails()
    {
        var validator = new VerifyOtpCommandValidator();
        var result = validator.TestValidate(new VerifyOtpCommand(
            "+919876543210",
            "abcdef",
            "device-1",
            DevicePlatform.Android,
            null,
            null,
            null,
            null));

        result.ShouldHaveValidationErrorFor(x => x.Code);
    }
}
