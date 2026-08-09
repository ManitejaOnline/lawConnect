namespace LawConnect.Api.Configuration;

/// <summary>
/// Maps dedicated Emergency environment variable names onto the Emergency options section.
/// </summary>
public static class EmergencyEnvironmentExtensions
{
    /// <summary>
    /// Applies <c>EMERGENCY_RADII</c> and <c>EMERGENCY_ROUND_SECONDS</c>.
    /// </summary>
    public static void AddEmergencyEnvironmentOverrides(this ConfigurationManager configuration)
    {
        var map = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        var radii = Environment.GetEnvironmentVariable("EMERGENCY_RADII");
        var timeout = Environment.GetEnvironmentVariable("EMERGENCY_ROUND_SECONDS")
                      ?? Environment.GetEnvironmentVariable("EMERGENCY_ACCEPTANCE_TIMEOUT_SECONDS");
        var allowIntl = Environment.GetEnvironmentVariable("EMERGENCY_ALLOW_INTERNATIONAL");

        if (!string.IsNullOrWhiteSpace(radii))
        {
            map["Emergency:Radii"] = radii.Trim();
        }

        if (!string.IsNullOrWhiteSpace(allowIntl))
        {
            map["Emergency:AllowInternational"] = allowIntl.Trim();
        }

        if (!string.IsNullOrWhiteSpace(timeout))
        {
            map["Emergency:AcceptanceTimeoutSeconds"] = timeout.Trim();
        }

        if (map.Count > 0)
        {
            configuration.AddInMemoryCollection(map);
        }
    }
}