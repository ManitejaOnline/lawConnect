namespace LawConnect.Domain.Enums;

/// <summary>
/// Admin review status for a lawyer complaint.
/// </summary>
public enum ComplaintStatus
{
    /// <summary>Submitted, awaiting admin review.</summary>
    Pending = 0,

    /// <summary>Admin confirmed the complaint is valid — affects Trust Score.</summary>
    Verified = 1,

    /// <summary>Admin found the complaint unfounded — never affects Trust Score.</summary>
    Dismissed = 2
}
