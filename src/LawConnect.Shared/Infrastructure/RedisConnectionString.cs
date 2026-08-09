namespace LawConnect.Shared.Infrastructure;

/// <summary>
/// Normalizes Redis connection strings for StackExchange.Redis, including Render <c>redis://</c> URLs.
/// </summary>
public static class RedisConnectionString
{
    /// <summary>
    /// Converts a <c>redis://</c> or <c>rediss://</c> URL into a StackExchange.Redis configuration string.
    /// Existing host:port or SE.Redis option strings are returned unchanged.
    /// </summary>
    /// <param name="value">Raw connection string or URL from configuration.</param>
    /// <returns>A configuration string accepted by StackExchange.Redis.</returns>
    public static string Normalize(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        if (!value.StartsWith("redis://", StringComparison.OrdinalIgnoreCase)
            && !value.StartsWith("rediss://", StringComparison.OrdinalIgnoreCase))
        {
            return value.Trim();
        }

        var uri = new Uri(value);
        var ssl = uri.Scheme.Equals("rediss", StringComparison.OrdinalIgnoreCase);
        var port = uri.IsDefaultPort ? 6379 : uri.Port;
        var configuration = $"{uri.Host}:{port},abortConnect=false";

        if (ssl)
        {
            configuration += ",ssl=true";
        }

        if (!string.IsNullOrEmpty(uri.UserInfo))
        {
            var userInfo = uri.UserInfo.Split(':', 2);
            var password = userInfo.Length == 2
                ? Uri.UnescapeDataString(userInfo[1])
                : Uri.UnescapeDataString(userInfo[0]);
            if (!string.IsNullOrEmpty(password))
            {
                configuration += $",password={password}";
            }
        }

        return configuration;
    }
}
