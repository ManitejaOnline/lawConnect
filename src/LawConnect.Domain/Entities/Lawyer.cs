using LawConnect.Domain.Common;
using LawConnect.Domain.Enums;

namespace LawConnect.Domain.Entities;

public sealed class Lawyer : BaseEntity
{
    public Guid UserId { get; set; }
    public string? BarCouncilNumber { get; set; }
    public string? StateBarCouncil { get; set; }
    public string? Bio { get; set; }
    public int YearsOfExperience { get; set; }
    public decimal ConsultationFee { get; set; }
    public decimal? EmergencyFee { get; set; }
    public string? CityName { get; set; }
    public string? StateName { get; set; }
    public string? CountryName { get; set; }
    public bool IsVerified { get; set; }
    public bool IsAvailableForEmergency { get; set; }
    public bool IsAcceptingAppointments { get; set; }
    public bool IsOnline { get; set; }
    public DateTime? LastOnlineAtUtc { get; set; }
    public double RatingAverage { get; set; }
    public int RatingCount { get; set; }
    public VerificationStatus VerificationStatus { get; set; } = VerificationStatus.Pending;
    public DateTime? VerifiedAtUtc { get; set; }
    public DateTime? ApplicationSubmittedAtUtc { get; set; }
    public string? ReviewReason { get; set; }
    public DateTime? ReviewedAtUtc { get; set; }
    public Guid? ReviewedByUserId { get; set; }

    public User User { get; set; } = null!;

    public ICollection<LawyerSpecialization> LawyerSpecializations { get; set; } = new List<LawyerSpecialization>();
    public ICollection<LawyerAvailability> Availabilities { get; set; } = new List<LawyerAvailability>();
    public ICollection<LawyerEducation> Educations { get; set; } = new List<LawyerEducation>();
    public ICollection<LawyerExperience> Experiences { get; set; } = new List<LawyerExperience>();
    public ICollection<LawyerLanguage> Languages { get; set; } = new List<LawyerLanguage>();
    public ICollection<LawyerDocument> Documents { get; set; } = new List<LawyerDocument>();
}
