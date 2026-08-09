using FluentValidation;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Auth.Models;
using LawConnect.Domain.Enums;
using MediatR;

namespace LawConnect.Application.Features.Auth.Commands.RefreshToken;

/// <summary>
/// Rotates a refresh token and issues a new token pair.
/// </summary>
public sealed record RefreshTokenCommand(
    string RefreshToken,
    string? DeviceIdentifier,
    DevicePlatform? Platform,
    string? FcmToken,
    string? AppVersion,
    string? OsVersion,
    string? IpAddress) : IRequest<Result<AuthTokenResponse>>;

/// <summary>
/// Validates <see cref="RefreshTokenCommand"/>.
/// </summary>
public sealed class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RefreshTokenCommandValidator"/> class.
    /// </summary>
    public RefreshTokenCommandValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty()
            .MinimumLength(32);
    }
}
