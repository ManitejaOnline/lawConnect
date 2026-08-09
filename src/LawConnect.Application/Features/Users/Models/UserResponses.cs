namespace LawConnect.Application.Features.Users.Models;

/// <summary>
/// Authenticated user profile response.
/// </summary>
public sealed record UserProfileResponse(
    Guid UserId,
    string PhoneNumber,
    string? Email,
    string FirstName,
    string LastName,
    string? ProfileImageUrl,
    bool IsPhoneVerified,
    DateOnly? DateOfBirth,
    string? Gender,
    string? PreferredLanguage,
    string[] Roles);

/// <summary>
/// Address response.
/// </summary>
public sealed record AddressResponse(
    Guid Id,
    string Line1,
    string? Line2,
    string? PostalCode,
    string? Landmark,
    Guid? CityId,
    double? Latitude,
    double? Longitude,
    bool IsPrimary,
    string? Label);

/// <summary>
/// Device response.
/// </summary>
public sealed record DeviceResponse(
    Guid Id,
    string DeviceIdentifier,
    string? FcmToken,
    string Platform,
    string? AppVersion,
    string? OsVersion,
    bool IsActive,
    DateTime? LastSeenAtUtc);

/// <summary>
/// Notification channel settings response.
/// </summary>
public sealed record NotificationSettingsResponse(
    bool EnablePush,
    bool EnableSms,
    bool EnableEmail,
    bool EnableInApp);
