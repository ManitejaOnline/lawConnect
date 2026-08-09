namespace LawConnect.Application.Features.Lawyers.Services;

/// <summary>
/// Cache key helpers for lawyer profiles.
/// </summary>
public static class LawyerCacheKeys
{
    /// <summary>
    /// Builds the cache key for a public lawyer profile.
    /// </summary>
    public static string Profile(Guid lawyerId) => $"lawyer:profile:{lawyerId:D}";
}
