namespace LawConnect.Shared.Infrastructure;

/// <summary>
/// Normalizes PostgreSQL connection strings for Npgsql, including Render <c>postgres://</c> URLs.
/// </summary>
public static class PostgresConnectionString
{
    /// <summary>
    /// Converts a <c>postgres://</c> or <c>postgresql://</c> URL into an Npgsql keyword connection string.
    /// Existing <c>Host=</c> strings are returned unchanged.
    /// </summary>
    /// <param name="value">Raw connection string or URL from configuration.</param>
    /// <returns>A connection string accepted by Npgsql and Hangfire PostgreSQL storage.</returns>
    public static string Normalize(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        var trimmed = value.Trim();
        if (!trimmed.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase)
            && !trimmed.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
        {
            return trimmed;
        }

        var uri = new Uri(trimmed);
        var userInfo = uri.UserInfo.Split(':', 2);
        var username = userInfo.Length > 0 ? Uri.UnescapeDataString(userInfo[0]) : string.Empty;
        var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : string.Empty;
        var database = Uri.UnescapeDataString(uri.AbsolutePath.Trim('/'));
        var port = uri.IsDefaultPort ? 5432 : uri.Port;
        var sslMode = "Require";

        var query = uri.Query.TrimStart('?');
        if (!string.IsNullOrEmpty(query))
        {
            foreach (var part in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                var kv = part.Split('=', 2);
                if (kv.Length == 2 && kv[0].Equals("sslmode", StringComparison.OrdinalIgnoreCase))
                {
                    sslMode = MapSslMode(Uri.UnescapeDataString(kv[1]));
                }
            }
        }

        return $"Host={uri.Host};Port={port};Database={database};Username={username};Password={password};SSL Mode={sslMode};Trust Server Certificate=true";
    }

    private static string MapSslMode(string sslMode)
    {
        return sslMode.ToLowerInvariant() switch
        {
            "disable" => "Disable",
            "allow" => "Allow",
            "prefer" => "Prefer",
            "require" => "Require",
            "verify-ca" => "VerifyCA",
            "verify-full" => "VerifyFull",
            _ => "Require"
        };
    }
}
