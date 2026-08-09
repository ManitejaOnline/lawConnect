using System.ComponentModel.DataAnnotations;

namespace LawConnect.Shared.Options;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required]
    [MinLength(32)]
    public string Secret { get; set; } = string.Empty;

    [Required]
    public string Issuer { get; set; } = string.Empty;

    [Required]
    public string Audience { get; set; } = string.Empty;

    [Range(1, 1440)]
    public int AccessTokenExpirationMinutes { get; set; } = 15;

    [Range(1, 90)]
    public int RefreshTokenExpirationDays { get; set; } = 7;
}
