namespace LawConnect.Domain.Enums;

/// <summary>
/// Severity of a verified lawyer complaint, set by the reviewing admin. Only meaningful once
/// the complaint's status is Verified.
/// </summary>
public enum ComplaintSeverity
{
    /// <summary>Small Trust Score deduction.</summary>
    Minor = 0,

    /// <summary>Larger Trust Score deduction.</summary>
    Serious = 1
}
