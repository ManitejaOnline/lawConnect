using System.Text.RegularExpressions;
using FluentValidation;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Auth.Models;
using LawConnect.Domain.Enums;
using MediatR;

namespace LawConnect.Application.Features.Auth.Commands.VerifyOtp;

/// <summary>
/// Verifies an OTP and issues authentication tokens.
/// </summary>
public sealed record VerifyOtpCommand(
    string PhoneNumber,
    string Code,
    string DeviceIdentifier,
    DevicePlatform Platform,
    string? FcmToken,
    string? AppVersion,
    string? OsVersion,
    string? IpAddress) : IRequest<Result<AuthLoginResponse>>;

/// <summary>
/// Validates <see cref="VerifyOtpCommand"/>.
/// </summary>
public sealed class VerifyOtpCommandValidator : AbstractValidator<VerifyOtpCommand>
{
    private static readonly Regex PhoneRegex = new(@"^\+[1-9]\d{7,14}$", RegexOptions.Compiled);

    /// <summary>
    /// Initializes a new instance of the <see cref="VerifyOtpCommandValidator"/> class.
    /// </summary>
    public VerifyOtpCommandValidator()
    {
        RuleFor(x => x.PhoneNumber)
            .NotEmpty()
            .Must(phone => PhoneRegex.IsMatch(phone.Trim()))
            .WithMessage("Phone number must be in E.164 format.");

        RuleFor(x => x.Code)
            .NotEmpty()
            .Length(4, 8)
            .Matches(@"^\d+$")
            .WithMessage("OTP code must be numeric.");

        RuleFor(x => x.DeviceIdentifier)
            .NotEmpty()
            .MaximumLength(256);

        RuleFor(x => x.Platform)
            .IsInEnum();
    }
}
