using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using LawConnect.Application.Common.Interfaces;
using LawConnect.Shared.Constants;
using LawConnect.Shared.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace LawConnect.Infrastructure.Auth;

/// <summary>
/// Creates signed JWT access tokens.
/// </summary>
public sealed class JwtTokenService : IJwtTokenService
{
    private readonly JwtOptions _options;
    private readonly IDateTimeProvider _dateTimeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="JwtTokenService"/> class.
    /// </summary>
    public JwtTokenService(IOptions<JwtOptions> options, IDateTimeProvider dateTimeProvider)
    {
        _options = options.Value;
        _dateTimeProvider = dateTimeProvider;
    }

    /// <inheritdoc />
    public (string AccessToken, DateTime ExpiresAtUtc) CreateAccessToken(
        Guid userId,
        string phoneNumber,
        IReadOnlyCollection<string> roles)
    {
        var expiresAt = _dateTimeProvider.UtcNow.AddMinutes(_options.AccessTokenExpirationMinutes);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new(AuthConstants.PhoneClaim, phoneNumber)
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: _dateTimeProvider.UtcNow,
            expires: expiresAt,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}
