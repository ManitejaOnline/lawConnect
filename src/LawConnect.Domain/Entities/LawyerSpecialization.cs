using LawConnect.Domain.Common;

namespace LawConnect.Domain.Entities;

public sealed class LawyerSpecialization : BaseEntity
{
    public Guid LawyerId { get; set; }
    public Guid SpecializationId { get; set; }
    public int YearsOfExperience { get; set; }
    public bool IsPrimary { get; set; }

    public Lawyer Lawyer { get; set; } = null!;
    public Specialization Specialization { get; set; } = null!;
}
