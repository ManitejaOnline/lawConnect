using FluentValidation.TestHelper;
using LawConnect.Application.Features.Auth.Commands.LoginWithPassword;
using LawConnect.Application.Features.Auth.Commands.RegisterCustomer;
using LawConnect.Application.Features.Auth.Services;
using LawConnect.Application.Features.Lawyers.Services;
using LawConnect.Domain.Entities;
using LawConnect.Domain.Enums;
using LawConnect.Infrastructure.Auth;

namespace LawConnect.Tests.Unit.Auth;

public sealed class PasswordRegistrationTests
{
    [Fact]
    public void RegisterCustomer_InvalidPhone_Fails()
    {
        var validator = new RegisterCustomerCommandValidator();
        var result = validator.TestValidate(new RegisterCustomerCommand(
            "Ada Lovelace",
            "ada@example.com",
            "9876543210",
            "Password1!",
            null,
            null,
            null,
            null,
            null,
            null));
        result.ShouldHaveValidationErrorFor(x => x.PhoneNumber);
    }

    [Fact]
    public void Login_EmptyIdentifier_Fails()
    {
        var validator = new LoginWithPasswordCommandValidator();
        var result = validator.TestValidate(new LoginWithPasswordCommand(
            "",
            "Password1!",
            null,
            null,
            null,
            null,
            null,
            null));
        result.ShouldHaveValidationErrorFor(x => x.Identifier);
    }

    [Fact]
    public void PasswordHasher_RoundTrips()
    {
        var hasher = new AspNetPasswordHasher();
        var hash = hasher.Hash("Password1!");
        Assert.True(hasher.Verify(hash, "Password1!"));
        Assert.False(hasher.Verify(hash, "wrong"));
    }

    [Fact]
    public void SplitFullName_UsesRemainderAsLastName()
    {
        var (first, last) = AuthAccountHelper.SplitFullName("Priya Nair Sharma");
        Assert.Equal("Priya", first);
        Assert.Equal("Nair Sharma", last);
    }

    [Fact]
    public void OperationalRules_OnlyApprovedIsOperational()
    {
        var pending = new Lawyer { IsVerified = false, VerificationStatus = VerificationStatus.Pending };
        var approved = new Lawyer { IsVerified = true, VerificationStatus = VerificationStatus.Approved };
        Assert.False(LawyerOperationalRules.IsApproved(pending));
        Assert.True(LawyerOperationalRules.IsApproved(approved));
    }
}
