namespace LawConnect.Domain.Enums;

/// <summary>
/// Lifecycle status for a legal case.
/// </summary>
public enum CaseStatus
{
    /// <summary>Created but not yet actively worked.</summary>
    Pending = 0,

    /// <summary>Actively being worked.</summary>
    Active = 1,

    /// <summary>Temporarily paused.</summary>
    OnHold = 2,

    /// <summary>Concluded in the client's favor.</summary>
    Won = 3,

    /// <summary>Concluded against the client.</summary>
    Lost = 4,

    /// <summary>Concluded by settlement between parties.</summary>
    Settled = 5,

    /// <summary>Concluded with no win/loss/settlement classification (e.g. non-litigation matters).</summary>
    Closed = 6,

    /// <summary>Withdrawn before conclusion.</summary>
    Cancelled = 7
}
