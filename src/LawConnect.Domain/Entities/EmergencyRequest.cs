using LawConnect.Domain.Common;
using LawConnect.Domain.Enums;

namespace LawConnect.Domain.Entities;

/// <summary>
/// Customer-initiated emergency SOS request.
/// </summary>
public sealed class EmergencyRequest : BaseEntity
{
    public Guid CustomerId { get; set; }
    public Guid CategoryId { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public Guid? CustomerCountryId { get; set; }
    public Guid? CustomerStateId { get; set; }
    public Guid? CustomerCityId { get; set; }
    public string? CustomerCountryName { get; set; }
    public string? CustomerStateName { get; set; }
    public string? CustomerCityName { get; set; }
    public EmergencySearchScope CurrentSearchScope { get; set; } = EmergencySearchScope.City;
    public EmergencyStatus Status { get; set; } = EmergencyStatus.Created;
    public int AttemptNumber { get; set; } = 1;
    public double CurrentRadiusKm { get; set; }
    public Guid? AssignedLawyerId { get; set; }
    public Guid? AcceptedAssignmentId { get; set; }
    public DateTime? SearchStartedAtUtc { get; set; }
    public DateTime? SearchExpiresAtUtc { get; set; }
    public DateTime? AssignedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public DateTime? CancelledAtUtc { get; set; }
    public string? CancellationReason { get; set; }
    public string? Notes { get; set; }
    public decimal? EmergencyFeeSnapshot { get; set; }

    public Customer Customer { get; set; } = null!;
    public EmergencyCategory Category { get; set; } = null!;
    public Lawyer? AssignedLawyer { get; set; }
    public Payment? Payment { get; set; }

    public ICollection<EmergencyAssignment> Assignments { get; set; } = new List<EmergencyAssignment>();
    public ICollection<EmergencyTimelineEvent> Timeline { get; set; } = new List<EmergencyTimelineEvent>();
}
