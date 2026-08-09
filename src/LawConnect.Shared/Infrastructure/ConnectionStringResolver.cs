namespace LawConnect.Shared.Infrastructure;

/// <summary>
/// Resolves PostgreSQL and Redis connection strings for Production without logging secrets.
/// </summary>
public static class ConnectionStringResolver
{
    /// <summary>
    /// Returns the first non-empty, non-loopback value; otherwise the first non-empty value.
    /// </summary>
    /// <param name="values">Candidate connection strings in priority order.</param>
    /// <returns>The selected value, or <see langword="null"/>.</returns>
    public static string? Coalesce(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value) && !IsLoopback(value))
            {
                return value.Trim();
            }
        }

        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return null;
    }

    /// <summary>
    /// Detects localhost / 127.0.0.1 connection strings without printing them.
    /// </summary>
    /// <param name="value">Raw connection string or URL.</param>
    /// <returns><see langword="true"/> when the host is loopback.</returns>
    public static bool IsLoopback(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var v = value.Trim();
        if (v.Contains("127.0.0.1", StringComparison.OrdinalIgnoreCase)
            || v.Contains("[::1]", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return v.Contains("Host=localhost", StringComparison.OrdinalIgnoreCase)
               || v.Contains("host=localhost", StringComparison.OrdinalIgnoreCase)
               || v.StartsWith("localhost:", StringComparison.OrdinalIgnoreCase)
               || v.Contains("://localhost", StringComparison.OrdinalIgnoreCase)
               || v.Contains("@localhost", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Throws in Production when a required connection string is missing or still loopback.
    /// Does not include the connection string in the exception message.
    /// </summary>
    /// <param name="environmentVariableName">Render / .NET env var name to set.</param>
    /// <param name="value">Resolved connection string.</param>
    /// <param name="isProduction">Whether the host is Production.</param>
    public static void EnsureNotLoopbackInProduction(
        string environmentVariableName,
        string? value,
        bool isProduction)
    {
        if (!isProduction)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(value) || IsLoopback(value))
        {
            throw new InvalidOperationException(
                $"Production requires {environmentVariableName} to be set to the Render service URL. "
                + "localhost / 127.0.0.1 is not allowed.");
        }
    }
}
