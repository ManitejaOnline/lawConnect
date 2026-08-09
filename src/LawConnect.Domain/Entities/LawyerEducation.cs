using LawConnect.Domain.Common;

namespace LawConnect.Domain.Entities;

public sealed class LawyerEducation : BaseEntity
{
    public Guid LawyerId { get; set; }
    public string InstitutionName { get; set; } = string.Empty;
    public string Degree { get; set; } = string.Empty;
    public string? FieldOfStudy { get; set; }
    public int? StartYear { get; set; }
    public int? EndYear { get; set; }
    public string? Description { get; set; }

    public Lawyer Lawyer { get; set; } = null!;
}
