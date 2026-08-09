namespace LawConnect.Application.Common.Interfaces;

/// <summary>
/// Result of creating a client-side upload URL.
/// </summary>
/// <param name="StorageKey">Object key that will be written.</param>
/// <param name="UploadUrl">Time-limited upload URL.</param>
/// <param name="ExpiresAtUtc">Expiration timestamp.</param>
public sealed record PresignedUploadResult(string StorageKey, Uri UploadUrl, DateTime ExpiresAtUtc);

/// <summary>
/// Stores and retrieves binary objects (for example profile images and chat attachments).
/// </summary>
public interface IFileStorage
{
    /// <summary>
    /// Uploads a file and returns its storage key.
    /// </summary>
    Task<string> UploadAsync(
        Stream content,
        string fileName,
        string contentType,
        string folder,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an object by storage key.
    /// </summary>
    Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a time-limited URL for downloading an object.
    /// </summary>
    Task<Uri> GetPresignedUrlAsync(
        string storageKey,
        TimeSpan validFor,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a time-limited URL for uploading an object without exposing storage credentials.
    /// </summary>
    Task<PresignedUploadResult> CreatePresignedUploadAsync(
        string fileName,
        string contentType,
        string folder,
        TimeSpan validFor,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes content to an exact storage key (used by local signed-upload completion).
    /// </summary>
    Task WriteExactAsync(
        string storageKey,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Opens a readable stream for an object. Caller owns disposal.
    /// </summary>
    Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default);
}
