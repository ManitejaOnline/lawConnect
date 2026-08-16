namespace LawConnect.Domain.Enums;

/// <summary>
/// Relative priority of a legal case.
/// </summary>
public enum CasePriority
{
    /// <summary>Low priority.</summary>
    Low = 0,

    /// <summary>Standard priority.</summary>
    Medium = 1,

    /// <summary>High priority.</summary>
    High = 2,

    /// <summary>Urgent, time-sensitive priority.</summary>
    Urgent = 3
}
