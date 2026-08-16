namespace LawConnect.Domain.Enums;

/// <summary>
/// Lifecycle status of a case hearing.
/// </summary>
public enum HearingStatus
{
    /// <summary>Booked and awaiting the scheduled date.</summary>
    Scheduled = 0,

    /// <summary>Took place; outcome recorded.</summary>
    Completed = 1,

    /// <summary>Postponed by the court.</summary>
    Adjourned = 2,

    /// <summary>Called off before taking place.</summary>
    Cancelled = 3
}
