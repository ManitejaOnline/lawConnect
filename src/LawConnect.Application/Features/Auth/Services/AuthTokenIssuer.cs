using LawConnect.Application.Common.Interfaces;
using LawConnect.Application.Features.Auth.Models;
using LawConnect.Domain.Entities;
using LawConnect.Domain.Enums;
using LawConnect.Shared.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LawConnect.Application.Features.Auth.Services;

/// <summary>
/// Shared authentication operations used by multiple auth handlers.
/// </summary>
public interface IAuthTokenIssuer
{
    /// <summary>
    /// Issues a JWT/refresh-token pair and optionally upserts device tracking.
    /// </summary>
    Task<AuthTokenResponse> IssueTokensAsync(
        User user,
        IReadOnlyCollection<string> roles,
        string? ipAddress,
        DeviceUpsertRequest? device,
        CancellationToken cancellationToken);
}

/// <summary>
/// Device fields captured during authentication.
/// </summary>
/// <param name="DeviceIdentifier">Client device identifier.</param>
/// <param name="Platform">Device platform.</param>
/// <param name="FcmToken">Optional push token.</param>
/// <param name="AppVersion">Optional app version.</param>
/// <param name="OsVersion">Optional OS version.</param>
public sealed record DeviceUpsertRequest(
    string DeviceIdentifier,
    DevicePlatform Platform,
    string? FcmToken,
    string? AppVersion,
    string? OsVersion);

/// <summary>
/// Default implementation of <see cref="IAuthTokenIssuer"/>.
/// </summary>
public sealed class AuthTokenIssuer : IAuthTokenIssuer
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ITokenHasher _tokenHasher;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly JwtOptions _jwtOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthTokenIssuer"/> class.
    /// </summary>
    public AuthTokenIssuer(
        IApplicationDbContext dbContext,
        IJwtTokenService jwtTokenService,
        ITokenHasher tokenHasher,
        IDateTimeProvider dateTimeProvider,
        IOptions<JwtOptions> jwtOptions)
    {
        _dbContext = dbContext;
        _jwtTokenService = jwtTokenService;
        _tokenHasher = tokenHasher;
        _dateTimeProvider = dateTimeProvider;
        _jwtOptions = jwtOptions.Value;
    }

    /// <inheritdoc />
    public async Task<AuthTokenResponse> IssueTokensAsync(
        User user,
        IReadOnlyCollection<string> roles,
        string? ipAddress,
        DeviceUpsertRequest? device,
        CancellationToken cancellationToken)
    {
        var (accessToken, accessExpiresAt) = _jwtTokenService.CreateAccessToken(
            user.Id,
            user.PhoneNumber,
            roles);

        var refreshTokenValue = _tokenHasher.GenerateOpaqueToken();
        var refreshExpiresAt = _dateTimeProvider.UtcNow.AddDays(_jwtOptions.RefreshTokenExpirationDays);

        _dbContext.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            Token = refreshTokenValue,
            ExpiresAtUtc = refreshExpiresAt,
            CreatedByIp = ipAddress
        });

        if (device is not null)
        {
            await UpsertDeviceAsync(user.Id, device, cancellationToken);
        }

        user.LastLoginAtUtc = _dateTimeProvider.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new AuthTokenResponse(
            accessToken,
            accessExpiresAt,
            refreshTokenValue,
            refreshExpiresAt);
    }

    private async Task UpsertDeviceAsync(
        Guid userId,
        DeviceUpsertRequest device,
        CancellationToken cancellationToken)
    {
        var existing = await _dbContext.Devices
            .FirstOrDefaultAsync(
                d => d.UserId == userId && d.DeviceIdentifier == device.DeviceIdentifier,
                cancellationToken);

        if (existing is null)
        {
            _dbContext.Devices.Add(new Device
            {
                UserId = userId,
                DeviceIdentifier = device.DeviceIdentifier,
                Platform = device.Platform,
                FcmToken = device.FcmToken,
                AppVersion = device.AppVersion,
                OsVersion = device.OsVersion,
                IsActive = true,
                LastSeenAtUtc = _dateTimeProvider.UtcNow
            });
            return;
        }

        existing.Platform = device.Platform;
        existing.FcmToken = device.FcmToken ?? existing.FcmToken;
        existing.AppVersion = device.AppVersion ?? existing.AppVersion;
        existing.OsVersion = device.OsVersion ?? existing.OsVersion;
        existing.IsActive = true;
        existing.LastSeenAtUtc = _dateTimeProvider.UtcNow;
    }
}
