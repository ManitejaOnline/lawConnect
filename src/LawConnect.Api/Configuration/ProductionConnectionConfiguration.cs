using LawConnect.Shared.Infrastructure;

namespace LawConnect.Api.Configuration;

/// <summary>
/// Maps Render-style URLs onto the existing ConnectionStrings section.
/// </summary>
public static class ProductionConnectionConfiguration
{
    /// <summary>
    /// If HangfireConnection is empty or loopback, reuse DefaultConnection.
    /// If DefaultConnection is empty or loopback, accept <c>DATABASE_URL</c>.
    /// If Redis is empty or loopback, accept <c>REDIS_URL</c>.
    /// Never logs connection string values.
    /// </summary>
    /// <param name="builder">Configuration builder.</param>
    /// <returns>The same builder.</returns>
    public static IConfigurationBuilder AddProductionConnectionFallbacks(this IConfigurationBuilder builder)
    {
        var current = builder.Build();
        var updates = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        var defaultConnection = current.GetConnectionString("DefaultConnection");
        var databaseUrl = current["DATABASE_URL"];
        if (IsUnusable(defaultConnection) && !string.IsNullOrWhiteSpace(databaseUrl))
        {
            updates["ConnectionStrings:DefaultConnection"] = databaseUrl.Trim();
            defaultConnection = databaseUrl.Trim();
        }

        var hangfireConnection = current.GetConnectionString("HangfireConnection");
        var effectiveDefault = updates.TryGetValue("ConnectionStrings:DefaultConnection", out var mappedDefault)
            ? mappedDefault
            : defaultConnection;
        if (IsUnusable(hangfireConnection) && !IsUnusable(effectiveDefault))
        {
            updates["ConnectionStrings:HangfireConnection"] = effectiveDefault;
        }

        var redis = current.GetConnectionString("Redis");
        var redisUrl = current["REDIS_URL"];
        if (IsUnusable(redis) && !string.IsNullOrWhiteSpace(redisUrl))
        {
            updates["ConnectionStrings:Redis"] = redisUrl.Trim();
        }

        if (updates.Count == 0)
        {
            return builder;
        }

        return builder.AddInMemoryCollection(updates);
    }

    private static bool IsUnusable(string? value)
        => string.IsNullOrWhiteSpace(value) || ConnectionStringResolver.IsLoopback(value);
}
