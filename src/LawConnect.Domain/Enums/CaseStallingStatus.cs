namespace LawConnect.Domain.Enums;

/// <summary>
/// How long a non-terminal case has gone without meaningful lawyer activity. Computed only —
/// never persisted on <see cref="Entities.Case"/> itself, since it's a live function of the
/// case's current data, not a stored fact.
/// </summary>
public enum CaseStallingStatus
{
    /// <summary>Recent meaningful activity, or the case is closed/won/lost/settled/cancelled.</summary>
    None = 0,

    /// <summary>7-13 days without meaningful lawyer activity.</summary>
    Warning = 1,

    /// <summary>14-29 days without meaningful lawyer activity.</summary>
    Stalling = 2,

    /// <summary>30+ days without meaningful lawyer activity.</summary>
    SevereStalling = 3
}
