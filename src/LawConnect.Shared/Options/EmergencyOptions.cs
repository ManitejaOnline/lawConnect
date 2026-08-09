using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace LawConnect.Shared.Options;

/// <summary>
/// Emergency SOS matching and timeout options.
/// Dispatch escalates City → State → Country. Radii are deprecated.
/// </summary>
public sealed class EmergencyOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Emergency";

    /// <summary>Acceptance window per notify round in seconds (escalation trigger only).</summary>
    [Range(1, 300)]
    public int AcceptanceTimeoutSeconds { get; set; } = 30;

    /// <summary>When true, a fourth round notifies other countries. MVP default is false.</summary>
    public bool AllowInternational { get; set; }

    /// <summary>Deprecated. Kept for config compatibility; not used for eligibility.</summary>
    public string Radii { get; set; } = "10,25,50,100";

    /// <summary>Deprecated.</summary>
    public double[] RadiusKm { get; set; } = [];

    /// <summary>Maximum lawyers notified per geographic round.</summary>
    [Range(1, 50)]
    public int MaxLawyersPerAttempt { get; set; } = 20;

    /// <summary>Deprecated.</summary>
    public double InitialRadiusKm { get; set; } = 10;

    /// <summary>Deprecated.</summary>
    public double RadiusExpansionFactor { get; set; } = 2;

    /// <summary>Legacy max attempts. Ignored — SOS does not globally expire by attempt count.</summary>
    public int MaxAttempts { get; set; } = 0;

    /// <summary>Configured geographic groups in dispatch order (1=City, 2=State, 3=Country, 4=International).</summary>
    public IReadOnlyList<int> GetScopeNumbers()
    {
        var scopes = new List<int> { 1, 2, 3 };
        if (AllowInternational)
        {
            scopes.Add(4);
        }

        return scopes;
    }

    /// <summary>Geographic group number for a 1-based attempt. After the last group, stays at last.</summary>
    public int ScopeNumberForAttempt(int attemptNumber)
    {
        var scopes = GetScopeNumbers();
        var index = Math.Max(0, attemptNumber - 1);
        return index < scopes.Count ? scopes[index] : scopes[^1];
    }

    /// <summary>Whether another geographic group remains after this attempt.</summary>
    public bool HasNextScope(int attemptNumber) => attemptNumber < GetScopeNumbers().Count;

    /// <summary>Deprecated radius list. Not used for SOS eligibility.</summary>
    public IReadOnlyList<double> GetRadii()
    {
        var fromArray = (RadiusKm ?? [])
            .Where(v => v > 0)
            .Select(v => Math.Round(v, 2))
            .ToArray();
        if (fromArray.Length > 0)
        {
            return fromArray.Distinct().OrderBy(v => v).ToArray();
        }

        var parsed = (Radii ?? string.Empty)
            .Split([',', ';', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(part => double.TryParse(part, NumberStyles.Float, CultureInfo.InvariantCulture, out var km) ? km : 0)
            .Where(km => km > 0)
            .Select(km => Math.Round(km, 2))
            .Distinct()
            .OrderBy(km => km)
            .ToArray();

        if (parsed.Length > 0)
        {
            return parsed;
        }

        return InitialRadiusKm > 0 ? [InitialRadiusKm] : [10d];
    }

    /// <summary>Deprecated.</summary>
    public double RadiusForAttempt(int attemptNumber)
    {
        var radii = GetRadii();
        var index = Math.Max(0, attemptNumber - 1);
        return index < radii.Count ? radii[index] : radii[^1];
    }

    /// <summary>Deprecated. Use <see cref="HasNextScope"/>.</summary>
    public bool HasNextConfiguredRadius(int attemptNumber) => HasNextScope(attemptNumber);
}
