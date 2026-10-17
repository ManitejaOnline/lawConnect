using System.Globalization;
using LawConnect.Application.Common.Interfaces;

namespace LawConnect.Infrastructure.Storage;

/// <summary>
/// Issues and verifies expiring HMAC signatures for local-storage upload and download URLs,
/// mirroring the guarantees of S3 presigned URLs.
/// </summary>
public sealed class LocalStorageUrlSigner
{
    private readonly ITokenHasher _hasher;
    private readonly IDateTimeProvider _clock;

    /// <summary>
    /// Initializes a new instance of the <see cref="LocalStorageUrlSigner"/> class.
    /// </summary>
    public LocalStorageUrlSigner(ITokenHasher hasher, IDateTimeProvider clock)
    {
        _hasher = hasher;
        _clock = clock;
    }

    /// <summary>Builds a signed relative upload URL for an exact storage key.</summary>
    public Uri CreateUploadUrl(string storageKey, string contentType, long maxBytes, DateTimeOffset expiresAt)
    {
        var expires = expiresAt.ToUnixTimeSeconds();
        var signature = _hasher.Hash(UploadPayload(storageKey, contentType, maxBytes, expires));
        var url = string.Concat(
            "/api/storage/uploads/",
            Uri.EscapeDataString(storageKey),
            "?expires=", expires.ToString(CultureInfo.InvariantCulture),
            "&ct=", Uri.EscapeDataString(contentType),
            "&max=", maxBytes.ToString(CultureInfo.InvariantCulture),
            "&sig=", signature);
        return new Uri(url, UriKind.Relative);
    }

    /// <summary>Builds a signed relative download URL for an exact storage key.</summary>
    public Uri CreateDownloadUrl(string storageKey, DateTimeOffset expiresAt)
    {
        var expires = expiresAt.ToUnixTimeSeconds();
        var signature = _hasher.Hash(DownloadPayload(storageKey, expires));
        var url = string.Concat(
            "/api/storage/files/",
            Uri.EscapeDataString(storageKey),
            "?expires=", expires.ToString(CultureInfo.InvariantCulture),
            "&sig=", signature);
        return new Uri(url, UriKind.Relative);
    }

    /// <summary>Verifies an upload signature and expiry.</summary>
    public bool IsValidUpload(string storageKey, string contentType, long maxBytes, long expires, string? signature) =>
        !IsExpired(expires)
        && !string.IsNullOrWhiteSpace(signature)
        && _hasher.Verify(UploadPayload(storageKey, contentType, maxBytes, expires), signature);

    /// <summary>Verifies a download signature and expiry.</summary>
    public bool IsValidDownload(string storageKey, long expires, string? signature) =>
        !IsExpired(expires)
        && !string.IsNullOrWhiteSpace(signature)
        && _hasher.Verify(DownloadPayload(storageKey, expires), signature);

    private bool IsExpired(long expires) =>
        new DateTimeOffset(DateTime.SpecifyKind(_clock.UtcNow, DateTimeKind.Utc)).ToUnixTimeSeconds() > expires;

    private static string UploadPayload(string storageKey, string contentType, long maxBytes, long expires) =>
        string.Join('|', "storage-upload", storageKey, contentType.ToLowerInvariant(), maxBytes, expires);

    private static string DownloadPayload(string storageKey, long expires) =>
        string.Join('|', "storage-download", storageKey, expires);
}
