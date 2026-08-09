namespace LawConnect.Shared.Constants;

/// <summary>
/// Authentication-related constant values.
/// </summary>
public static class AuthConstants
{
    /// <summary>
    /// Rate limiting policy applied to authentication endpoints.
    /// </summary>
    public const string RateLimitPolicy = "AuthEndpoints";

    /// <summary>
    /// JWT claim type for phone number.
    /// </summary>
    public const string PhoneClaim = "phone_number";

    /// <summary>
    /// Cache key prefix for OTP request rate limiting.
    /// </summary>
    public const string OtpRateLimitCachePrefix = "auth:otp:rate:";
}
