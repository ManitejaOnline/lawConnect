using LawConnect.Domain.Common;

namespace LawConnect.Domain.Entities;

public sealed class Specialization : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Slug { get; set; }
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; }

    public ICollection<LawyerSpecialization> LawyerSpecializations { get; set; } = new List<LawyerSpecialization>();
}
