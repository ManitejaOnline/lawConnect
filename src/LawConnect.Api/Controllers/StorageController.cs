using LawConnect.Application.Common.Interfaces;
using LawConnect.Infrastructure.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Net.Http.Headers;

namespace LawConnect.Api.Controllers;

/// <summary>
/// Upload and download endpoints backing local storage signed URLs.
/// </summary>
[ApiController]
[Authorize]
[Route("api/storage")]
public sealed class StorageController : ControllerBase
{
    private static readonly FileExtensionContentTypeProvider ContentTypes = new();

    private readonly IFileStorage _fileStorage;
    private readonly IVirusScanner _virusScanner;
    private readonly LocalStorageUrlSigner _signer;

    /// <summary>
    /// Initializes a new instance of the <see cref="StorageController"/> class.
    /// </summary>
    public StorageController(IFileStorage fileStorage, IVirusScanner virusScanner, LocalStorageUrlSigner signer)
    {
        _fileStorage = fileStorage;
        _virusScanner = virusScanner;
        _signer = signer;
    }

    /// <summary>
    /// Completes a local signed upload without exposing storage credentials.
    /// </summary>
    [HttpPut("uploads/{*storageKey}")]
    [EnableRateLimiting("UploadEndpoints")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status413PayloadTooLarge)]
    public async Task<IActionResult> Upload(
        string storageKey,
        [FromQuery] long expires,
        [FromQuery] string? ct,
        [FromQuery] long max,
        [FromQuery] string? sig,
        CancellationToken cancellationToken)
    {
        storageKey = Uri.UnescapeDataString(storageKey);
        if (!IsSafeKey(storageKey)
            || string.IsNullOrWhiteSpace(ct)
            || !_signer.IsValidUpload(storageKey, ct, max, expires, sig))
        {
            return Forbid();
        }

        var contentType = MediaTypeHeaderValue.TryParse(Request.ContentType, out var parsed)
            ? parsed.MediaType.ToString()
            : null;
        if (!string.Equals(contentType, ct, StringComparison.OrdinalIgnoreCase))
        {
            return Forbid();
        }

        if (Request.ContentLength > max)
        {
            return StatusCode(StatusCodes.Status413PayloadTooLarge);
        }

        await using var buffer = new MemoryStream();
        var chunk = new byte[81920];
        int read;
        while ((read = await Request.Body.ReadAsync(chunk, cancellationToken)) > 0)
        {
            if (buffer.Length + read > max)
            {
                return StatusCode(StatusCodes.Status413PayloadTooLarge);
            }

            buffer.Write(chunk, 0, read);
        }

        buffer.Position = 0;
        await _virusScanner.ScanAsync(buffer, Path.GetFileName(storageKey), ct, cancellationToken);
        buffer.Position = 0;
        await _fileStorage.WriteExactAsync(storageKey, buffer, ct, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Streams a file through a signed, expiring download URL (used for chat attachments).
    /// </summary>
    [AllowAnonymous]
    [HttpGet("files/{*storageKey}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Download(
        string storageKey,
        [FromQuery] long expires,
        [FromQuery] string? sig,
        CancellationToken cancellationToken)
    {
        storageKey = Uri.UnescapeDataString(storageKey);
        if (!IsSafeKey(storageKey) || !_signer.IsValidDownload(storageKey, expires, sig))
        {
            return NotFound();
        }

        Stream stream;
        try
        {
            stream = await _fileStorage.OpenReadAsync(storageKey, cancellationToken);
        }
        catch (FileNotFoundException)
        {
            return NotFound();
        }

        if (!ContentTypes.TryGetContentType(storageKey, out var contentType))
        {
            contentType = "application/octet-stream";
        }

        Response.Headers[HeaderNames.CacheControl] = "private, no-store";
        var inlineSafe = contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)
            && !contentType.Contains("svg", StringComparison.OrdinalIgnoreCase)
            || string.Equals(contentType, "application/pdf", StringComparison.OrdinalIgnoreCase);
        return inlineSafe
            ? File(stream, contentType, enableRangeProcessing: true)
            : File(stream, "application/octet-stream", Path.GetFileName(storageKey));
    }

    private static bool IsSafeKey(string storageKey)
    {
        var normalized = storageKey.Replace('\\', '/');
        return !string.IsNullOrWhiteSpace(normalized)
            && !normalized.StartsWith('/')
            && !normalized.Split('/').Any(segment => segment is "" or "." or "..")
            && !StoragePath.IsPrivateKey(normalized);
    }
}
