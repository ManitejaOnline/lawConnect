using System.ComponentModel.DataAnnotations;

namespace LawConnect.Shared.Options;

/// <summary>
/// Local/object storage configuration.
/// </summary>
public sealed class StorageOptions
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "Storage";

    /// <summary>
    /// Root directory for stored files (relative or absolute).
    /// </summary>
    [Required]
    [MinLength(1)]
    public string RootPath { get; set; } = "storage";

    /// <summary>
    /// Public URL prefix used when building download URLs.
    /// </summary>
    [Required]
    [MinLength(1)]
    public string PublicBaseUrl { get; set; } = "/files";

    /// <summary>
    /// Maximum upload size in bytes.
    /// </summary>
    [Range(1, long.MaxValue)]
    public long MaxUploadBytes { get; set; } = 10 * 1024 * 1024;
}
