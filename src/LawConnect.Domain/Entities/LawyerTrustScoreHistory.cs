using LawConnect.Domain.Common;

namespace LawConnect.Domain.Entities;

public sealed class LawyerTrustScoreHistory : BaseEntity
{
    public Guid LawyerId { get; set; }
    public int Score { get; set; }
    public string Level { get; set; } = string.Empty;
    public double ClientRatingPoints { get; set; }
    public double AppointmentReliabilityPoints { get; set; }
    public double ResponsivenessPoints { get; set; }
    public double CaseUpdatePoints { get; set; }
    public double CaseManagementPoints { get; set; }
    public double VerifiedComplaintPoints { get; set; }

    /// <summary>Human-readable explanation when the Case Management component included a stalling deduction, e.g. "Trust Score decreased by 4 because 2 cases had no meaningful activity for more than 14 days." Null when there was nothing to deduct.</summary>
    public string? CaseManagementNote { get; set; }

    public DateTime ComputedAtUtc { get; set; }

    public Lawyer Lawyer { get; set; } = null!;
}
