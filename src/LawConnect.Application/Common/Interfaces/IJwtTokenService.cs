namespace LawConnect.Application.Common.Interfaces;

/// <summary>
/// Issues JWT access tokens for authenticated users.
/// </summary>
public interface IJwtTokenService
{
    /// <summary>
    /// Creates a signed access token.
    /// </summary>
    /// <param name="userId">User identifier.</param>
    /// <param name="phoneNumber">User phone number.</param>
    /// <param name="roles">Assigned role names.</param>
    /// <returns>Token value and absolute UTC expiration.</returns>
    (string AccessToken, DateTime ExpiresAtUtc) CreateAccessToken(
        Guid userId,
        string phoneNumber,
        IReadOnlyCollection<string> roles);
}
