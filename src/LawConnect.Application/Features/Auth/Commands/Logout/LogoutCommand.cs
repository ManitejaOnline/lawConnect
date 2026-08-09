using FluentValidation;
using LawConnect.Application.Common.Models;
using MediatR;

namespace LawConnect.Application.Features.Auth.Commands.Logout;

/// <summary>
/// Revokes the provided refresh token (logout current session).
/// </summary>
/// <param name="RefreshToken">Refresh token to revoke.</param>
/// <param name="IpAddress">Caller IP address.</param>
public sealed record LogoutCommand(string RefreshToken, string? IpAddress) : IRequest<Result>;

/// <summary>
/// Validates <see cref="LogoutCommand"/>.
/// </summary>
public sealed class LogoutCommandValidator : AbstractValidator<LogoutCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LogoutCommandValidator"/> class.
    /// </summary>
    public LogoutCommandValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty()
            .MinimumLength(32);
    }
}
