using System.ComponentModel.DataAnnotations;

namespace LawConnect.Shared.Options;

public sealed class ConnectionStringsOptions
{
    public const string SectionName = "ConnectionStrings";

    [Required]
    public string DefaultConnection { get; set; } = string.Empty;

    [Required]
    public string Redis { get; set; } = string.Empty;

    [Required]
    public string HangfireConnection { get; set; } = string.Empty;
}
