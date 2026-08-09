using FluentValidation;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Auth.Models;
using LawConnect.Domain.Enums;
using MediatR;

namespace LawConnect.Application.Features.Auth.Commands.LoginWithPassword;

/// <summary>
/// Authenticates with email or phone plus password.
/// </summary>
public sealed record LoginWithPasswordCommand(
    string Identifier,
    string Password,
    string? DeviceIdentifier,
    DevicePlatform? Platform,
    string? FcmToken,
    string? AppVersion,
    string? OsVersion,
    string? IpAddress) : IRequest<Result<AuthLoginResponse>>;

/// <summary>
/// Validates <see cref="LoginWithPasswordCommand"/>.
/// </summary>
public sealed class LoginWithPasswordCommandValidator : AbstractValidator<LoginWithPasswordCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LoginWithPasswordCommandValidator"/> class.
    /// </summary>
    public LoginWithPasswordCommandValidator()
    {
        RuleFor(x => x.Identifier).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Password).NotEmpty().MaximumLength(128);
    }
}
