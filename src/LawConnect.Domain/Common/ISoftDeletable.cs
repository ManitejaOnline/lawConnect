namespace LawConnect.Domain.Common;

/// <summary>
/// Marks an entity that supports soft deletion.
/// </summary>
public interface ISoftDeletable
{
    /// <summary>Gets or sets a value indicating whether the entity is soft-deleted.</summary>
    bool IsDeleted { get; set; }

    /// <summary>Gets or sets the UTC deletion timestamp.</summary>
    DateTime? DeletedAt { get; set; }

    /// <summary>Gets or sets the user who soft-deleted the entity.</summary>
    string? DeletedBy { get; set; }
}
