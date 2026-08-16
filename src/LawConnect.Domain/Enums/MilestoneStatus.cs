namespace LawConnect.Domain.Enums;

/// <summary>
/// Lifecycle status of a case milestone.
/// </summary>
public enum MilestoneStatus
{
    /// <summary>Not yet started.</summary>
    Pending = 0,

    /// <summary>Actively being worked.</summary>
    InProgress = 1,

    /// <summary>Done.</summary>
    Completed = 2,

    /// <summary>Past its due date and not completed. Set by the overdue sweep, never by a client.</summary>
    Overdue = 3,

    /// <summary>No longer applicable.</summary>
    Cancelled = 4
}
