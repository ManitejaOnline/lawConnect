using System.ComponentModel.DataAnnotations;

namespace LawConnect.Shared.Options;

/// <summary>
/// Firebase Cloud Messaging options.
/// </summary>
public sealed class FirebaseOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Firebase";

    /// <summary>When false, push is logged instead of sent.</summary>
    public bool Enabled { get; set; }

    /// <summary>Path to Firebase service account JSON (optional for logging mode).</summary>
    public string? ServiceAccountPath { get; set; }

    /// <summary>Project id for diagnostics.</summary>
    public string? ProjectId { get; set; }
}
