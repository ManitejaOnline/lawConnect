using LawConnect.Domain.Common;

namespace LawConnect.Domain.Entities;

/// <summary>
/// Emergency category mapped to a legal specialization.
/// </summary>
public sealed class EmergencyCategory : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid SpecializationId { get; set; }
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; }

    public Specialization Specialization { get; set; } = null!;
}
