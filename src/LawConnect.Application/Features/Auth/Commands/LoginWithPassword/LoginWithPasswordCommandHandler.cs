using LawConnect.Application.Common.Interfaces;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Auth.Models;
using LawConnect.Application.Features.Auth.Services;
using LawConnect.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawConnect.Application.Features.Auth.Commands.LoginWithPassword;

/// <summary>
/// Handles <see cref="LoginWithPasswordCommand"/>.
/// </summary>
public sealed class LoginWithPasswordCommandHandler : IRequestHandler<LoginWithPasswordCommand, Result<AuthLoginResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAuthTokenIssuer _tokenIssuer;

    /// <summary>
    /// Initializes a new instance of the <see cref="LoginWithPasswordCommandHandler"/> class.
    /// </summary>
    public LoginWithPasswordCommandHandler(
        IApplicationDbContext dbContext,
        IPasswordHasher passwordHasher,
        IAuthTokenIssuer tokenIssuer)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _tokenIssuer = tokenIssuer;
    }

    /// <inheritdoc />
    public async Task<Result<AuthLoginResponse>> Handle(
        LoginWithPasswordCommand request,
        CancellationToken cancellationToken)
    {
        var identifier = request.Identifier.Trim();
        var user = identifier.Contains('@', StringComparison.Ordinal)
            ? await _dbContext.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Email == AuthAccountHelper.NormalizeEmail(identifier), cancellationToken)
            : await _dbContext.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.PhoneNumber == identifier, cancellationToken);

        if (user is null || string.IsNullOrWhiteSpace(user.PasswordHash)
            || !_passwordHasher.Verify(user.PasswordHash, request.Password))
        {
            throw new UnauthorizedException("Invalid email/phone or password.");
        }

        if (!user.IsActive)
        {
            throw new ForbiddenException("User account is disabled.");
        }

        var roles = user.UserRoles
            .Where(ur => !ur.IsDeleted)
            .Select(ur => ur.Role.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var tokens = await _tokenIssuer.IssueTokensAsync(
            user,
            roles,
            request.IpAddress,
            AuthAccountHelper.ToDevice(
                request.DeviceIdentifier,
                request.Platform,
                request.FcmToken,
                request.AppVersion,
                request.OsVersion),
            cancellationToken);

        return Result.Success(new AuthLoginResponse(user.Id, user.PhoneNumber, roles, false, tokens));
    }
}
