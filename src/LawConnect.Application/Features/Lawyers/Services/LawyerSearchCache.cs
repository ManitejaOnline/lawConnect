using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Lawyers.Models;
using Microsoft.Extensions.Caching.Distributed;

namespace LawConnect.Application.Features.Lawyers.Services;

/// <summary>
/// Cache for lawyer discovery result pages.
/// </summary>
public interface ILawyerSearchCache
{
    /// <summary>Gets a cached search page.</summary>
    Task<PagedResult<LawyerSearchItemResponse>?> GetAsync(string cacheKey, CancellationToken cancellationToken);

    /// <summary>Stores a search page.</summary>
    Task SetAsync(string cacheKey, PagedResult<LawyerSearchItemResponse> page, CancellationToken cancellationToken);

    /// <summary>Builds a deterministic cache key for search filters.</summary>
    string BuildKey(object filters);

    /// <summary>Bumps the search cache generation so approved/rejected lawyers appear immediately.</summary>
    Task InvalidateAllAsync(CancellationToken cancellationToken);
}

/// <summary>
/// Default <see cref="ILawyerSearchCache"/> implementation.
/// </summary>
public sealed class LawyerSearchCache : ILawyerSearchCache
{
    private const string VersionKey = "lawyer:search:version";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly DistributedCacheEntryOptions CacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1)
    };

    private readonly IDistributedCache _cache;

    /// <summary>
    /// Initializes a new instance of the <see cref="LawyerSearchCache"/> class.
    /// </summary>
    public LawyerSearchCache(IDistributedCache cache) => _cache = cache;

    /// <inheritdoc />
    public async Task<PagedResult<LawyerSearchItemResponse>?> GetAsync(
        string cacheKey,
        CancellationToken cancellationToken)
    {
        var bytes = await _cache.GetAsync(await VersionedKeyAsync(cacheKey, cancellationToken), cancellationToken);
        if (bytes is null)
        {
            return null;
        }

        var dto = JsonSerializer.Deserialize<LawyerSearchPageDto>(bytes, JsonOptions);
        return dto is null
            ? null
            : new PagedResult<LawyerSearchItemResponse>(dto.Items, dto.TotalCount, dto.PageNumber, dto.PageSize);
    }

    /// <inheritdoc />
    public async Task SetAsync(
        string cacheKey,
        PagedResult<LawyerSearchItemResponse> page,
        CancellationToken cancellationToken)
    {
        var dto = new LawyerSearchPageDto(page.Items.ToList(), page.TotalCount, page.PageNumber, page.PageSize);
        var bytes = JsonSerializer.SerializeToUtf8Bytes(dto, JsonOptions);
        await _cache.SetAsync(await VersionedKeyAsync(cacheKey, cancellationToken), bytes, CacheOptions, cancellationToken);
    }

    /// <inheritdoc />
    public string BuildKey(object filters)
    {
        var json = JsonSerializer.Serialize(filters, JsonOptions);
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json)));
        return $"lawyer:search:{hash}";
    }

    /// <inheritdoc />
    public async Task InvalidateAllAsync(CancellationToken cancellationToken)
    {
        var current = await _cache.GetStringAsync(VersionKey, cancellationToken);
        var next = (int.TryParse(current, out var version) ? version : 0) + 1;
        await _cache.SetStringAsync(VersionKey, next.ToString(), cancellationToken);
    }

    private async Task<string> VersionedKeyAsync(string cacheKey, CancellationToken cancellationToken)
    {
        var version = await _cache.GetStringAsync(VersionKey, cancellationToken) ?? "0";
        return $"{cacheKey}:v{version}";
    }

    private sealed record LawyerSearchPageDto(
        List<LawyerSearchItemResponse> Items,
        int TotalCount,
        int PageNumber,
        int PageSize);
}
