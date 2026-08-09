namespace LawConnect.Application.Features.Auth.Models;

/// <summary>
/// Authentication token pair returned after successful login or refresh.
/// </summary>
/// <param name="AccessToken">JWT access token.</param>
/// <param name="AccessTokenExpiresAtUtc">Access token expiration in UTC.</param>
/// <param name="RefreshToken">Opaque refresh token.</param>
/// <param name="RefreshTokenExpiresAtUtc">Refresh token expiration in UTC.</param>
public sealed record AuthTokenResponse(
    string AccessToken,
    DateTime AccessTokenExpiresAtUtc,
    string RefreshToken,
    DateTime RefreshTokenExpiresAtUtc);

/// <summary>
/// Successful OTP verification / login response.
/// </summary>
/// <param name="UserId">Authenticated user identifier.</param>
/// <param name="PhoneNumber">Verified phone number.</param>
/// <param name="Roles">Assigned roles.</param>
/// <param name="IsNewUser">Whether the account was created during this login.</param>
/// <param name="Tokens">Issued token pair.</param>
public sealed record AuthLoginResponse(
    Guid UserId,
    string PhoneNumber,
    IReadOnlyCollection<string> Roles,
    bool IsNewUser,
    AuthTokenResponse Tokens);

/// <summary>
/// Response after requesting an OTP.
/// </summary>
/// <param name="Destination">Masked phone destination.</param>
/// <param name="ExpiresAtUtc">OTP expiration in UTC.</param>
/// <param name="ExpiresInSeconds">Seconds until expiration.</param>
public sealed record RequestOtpResponse(
    string Destination,
    DateTime ExpiresAtUtc,
    int ExpiresInSeconds);

/// <summary>
/// Authenticated user summary for session inspection.
/// </summary>
/// <param name="UserId">User identifier.</param>
/// <param name="PhoneNumber">Phone number.</param>
/// <param name="FirstName">First name.</param>
/// <param name="LastName">Last name.</param>
/// <param name="Roles">Assigned roles.</param>
public sealed record CurrentAuthUserResponse(
    Guid UserId,
    string PhoneNumber,
    string FirstName,
    string LastName,
    IReadOnlyCollection<string> Roles);
