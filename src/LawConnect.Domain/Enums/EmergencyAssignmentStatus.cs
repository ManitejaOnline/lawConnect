namespace LawConnect.Domain.Enums;

/// <summary>
/// Status of a lawyer assignment for an SOS request attempt.
/// </summary>
public enum EmergencyAssignmentStatus
{
    Pending = 0,
    Accepted = 1,
    Declined = 2,
    Cancelled = 3,
    Expired = 4
}
