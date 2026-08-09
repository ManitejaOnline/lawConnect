using System.Text.Json;
using LawConnect.Application.Features.Lawyers.Models;
using Microsoft.Extensions.Caching.Distributed;

namespace LawConnect.Application.Features.Lawyers.Services;

/// <summary>
/// Distributed cache helper for lawyer profiles.
/// </summary>
public interface ILawyerProfileCache
{
    /// <summary>
    /// Gets a cached profile.
    /// </summary>
    Task<LawyerProfileResponse?> GetAsync(Guid lawyerId, CancellationToken cancellationToken);

    /// <summary>
    /// Stores a profile in cache.
    /// </summary>
    Task SetAsync(LawyerProfileResponse profile, CancellationToken cancellationToken);

    /// <summary>
    /// Removes a cached profile.
    /// </summary>
    Task RemoveAsync(Guid lawyerId, CancellationToken cancellationToken);
}

/// <summary>
/// Default <see cref="ILawyerProfileCache"/> implementation.
/// </summary>
public sealed class LawyerProfileCache : ILawyerProfileCache
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly DistributedCacheEntryOptions CacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
    };

    private readonly IDistributedCache _cache;

    /// <summary>
    /// Initializes a new instance of the <see cref="LawyerProfileCache"/> class.
    /// </summary>
    public LawyerProfileCache(IDistributedCache cache)
    {
        _cache = cache;
    }

    /// <inheritdoc />
    public async Task<LawyerProfileResponse?> GetAsync(Guid lawyerId, CancellationToken cancellationToken)
    {
        var bytes = await _cache.GetAsync(LawyerCacheKeys.Profile(lawyerId), cancellationToken);
        return bytes is null ? null : JsonSerializer.Deserialize<LawyerProfileResponse>(bytes, JsonOptions);
    }

    /// <inheritdoc />
    public Task SetAsync(LawyerProfileResponse profile, CancellationToken cancellationToken)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(profile, JsonOptions);
        return _cache.SetAsync(LawyerCacheKeys.Profile(profile.LawyerId), bytes, CacheOptions, cancellationToken);
    }

    /// <inheritdoc />
    public Task RemoveAsync(Guid lawyerId, CancellationToken cancellationToken) =>
        _cache.RemoveAsync(LawyerCacheKeys.Profile(lawyerId), cancellationToken);
}
