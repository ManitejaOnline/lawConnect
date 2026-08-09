using System.ComponentModel.DataAnnotations;

namespace LawConnect.Shared.Options;

/// <summary>
/// OTP generation and rate-limit settings for passwordless login.
/// </summary>
public sealed class OtpOptions
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "Otp";

    /// <summary>
    /// Gets or sets the HMAC secret used to hash OTP codes.
    /// </summary>
    [Required]
    [MinLength(32)]
    public string HashSecret { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the OTP length.
    /// </summary>
    [Range(4, 8)]
    public int CodeLength { get; set; } = 6;

    /// <summary>
    /// Gets or sets OTP lifetime in minutes.
    /// </summary>
    [Range(1, 30)]
    public int ExpirationMinutes { get; set; } = 5;

    /// <summary>
    /// Gets or sets maximum verification attempts per OTP.
    /// </summary>
    [Range(1, 10)]
    public int MaxAttempts { get; set; } = 5;

    /// <summary>
    /// Gets or sets maximum OTP requests allowed per phone in the rate window.
    /// </summary>
    [Range(1, 20)]
    public int MaxRequestsPerWindow { get; set; } = 3;

    /// <summary>
    /// Gets or sets the OTP request rate-limit window in minutes.
    /// </summary>
    [Range(1, 60)]
    public int RequestWindowMinutes { get; set; } = 10;
}
