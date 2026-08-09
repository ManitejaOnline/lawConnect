using LawConnect.Domain.Common;

namespace LawConnect.Domain.Entities;

public sealed class LawyerExperience : BaseEntity
{
    public Guid LawyerId { get; set; }
    public string OrganizationName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public bool IsCurrent { get; set; }
    public string? Description { get; set; }

    public Lawyer Lawyer { get; set; } = null!;
}
