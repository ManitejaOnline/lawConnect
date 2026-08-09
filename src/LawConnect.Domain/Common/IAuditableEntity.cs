namespace LawConnect.Domain.Common;

/// <summary>
/// Marks an entity that tracks create/update audit metadata.
/// </summary>
public interface IAuditableEntity
{
    /// <summary>Gets or sets the UTC creation timestamp.</summary>
    DateTime CreatedAt { get; set; }

    /// <summary>Gets or sets the UTC last-update timestamp.</summary>
    DateTime? UpdatedAt { get; set; }

    /// <summary>Gets or sets the creator user identifier.</summary>
    string? CreatedBy { get; set; }

    /// <summary>Gets or sets the last modifier user identifier.</summary>
    string? UpdatedBy { get; set; }
}
