using LawConnect.Application.Common.Interfaces;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Auth.Models;
using LawConnect.Application.Features.Auth.Services;
using LawConnect.Domain.Enums;
using LawConnect.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawConnect.Application.Features.Auth.Commands.RefreshToken;

/// <summary>
/// Handles refresh-token rotation with reuse detection.
/// </summary>
public sealed class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, Result<AuthTokenResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IAuthTokenIssuer _authTokenIssuer;
    private readonly ITokenHasher _tokenHasher;

    /// <summary>
    /// Initializes a new instance of the <see cref="RefreshTokenCommandHandler"/> class.
    /// </summary>
    public RefreshTokenCommandHandler(
        IApplicationDbContext dbContext,
        IDateTimeProvider dateTimeProvider,
        IAuthTokenIssuer authTokenIssuer,
        ITokenHasher tokenHasher)
    {
        _dbContext = dbContext;
        _dateTimeProvider = dateTimeProvider;
        _authTokenIssuer = authTokenIssuer;
        _tokenHasher = tokenHasher;
    }

    /// <inheritdoc />
    public async Task<Result<AuthTokenResponse>> Handle(
        RefreshTokenCommand request,
        CancellationToken cancellationToken)
    {
        var utcNow = _dateTimeProvider.UtcNow;

        var existingToken = await _dbContext.RefreshTokens
            .Include(t => t.User)
            .ThenInclude(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(t => t.Token == _tokenHasher.Hash(request.RefreshToken), cancellationToken);

        if (existingToken is null)
        {
            throw new UnauthorizedException("Invalid refresh token.");
        }

        if (existingToken.RevokedAtUtc.HasValue)
        {
            await RevokeAllUserTokensAsync(existingToken.UserId, request.IpAddress, utcNow, cancellationToken);
            throw new UnauthorizedException("Refresh token reuse detected. All sessions have been revoked.");
        }

        if (existingToken.ExpiresAtUtc <= utcNow)
        {
            throw new UnauthorizedException("Refresh token has expired.");
        }

        if (!existingToken.User.IsActive)
        {
            throw new ForbiddenException("User account is disabled.");
        }

        var roles = existingToken.User.UserRoles
            .Where(ur => !ur.IsDeleted)
            .Select(ur => ur.Role.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        DeviceUpsertRequest? device = null;
        if (!string.IsNullOrWhiteSpace(request.DeviceIdentifier))
        {
            device = new DeviceUpsertRequest(
                request.DeviceIdentifier,
                request.Platform ?? DevicePlatform.Unknown,
                request.FcmToken,
                request.AppVersion,
                request.OsVersion);
        }

        existingToken.RevokedAtUtc = utcNow;
        existingToken.RevokedByIp = request.IpAddress;

        var tokens = await _authTokenIssuer.IssueTokensAsync(
            existingToken.User,
            roles,
            request.IpAddress,
            device,
            cancellationToken);

        existingToken.ReplacedByToken = _tokenHasher.Hash(tokens.RefreshToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(tokens);
    }

    private async Task RevokeAllUserTokensAsync(
        Guid userId,
        string? ipAddress,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        var tokens = await _dbContext.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAtUtc == null)
            .ToListAsync(cancellationToken);

        foreach (var token in tokens)
        {
            token.RevokedAtUtc = utcNow;
            token.RevokedByIp = ipAddress;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
