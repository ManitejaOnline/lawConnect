using System.ComponentModel.DataAnnotations;

namespace LawConnect.Shared.Options;

public sealed class CorsOptions
{
    public const string SectionName = "Cors";

    [Required]
    [MinLength(1)]
    public string[] AllowedOrigins { get; set; } = [];
}
