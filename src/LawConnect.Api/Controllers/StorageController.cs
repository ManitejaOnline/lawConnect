using LawConnect.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace LawConnect.Api.Controllers;

/// <summary>
/// Authenticated upload endpoint used by local storage signed URLs.
/// </summary>
[ApiController]
[Authorize]
[Route("api/storage")]
public sealed class StorageController : ControllerBase
{
    private readonly IFileStorage _fileStorage;
    private readonly IVirusScanner _virusScanner;

    /// <summary>
    /// Initializes a new instance of the <see cref="StorageController"/> class.
    /// </summary>
    public StorageController(IFileStorage fileStorage, IVirusScanner virusScanner)
    {
        _fileStorage = fileStorage;
        _virusScanner = virusScanner;
    }

    /// <summary>
    /// Completes a local signed upload without exposing storage credentials.
    /// </summary>
    [HttpPut("uploads/{*storageKey}")]
    [EnableRateLimiting("UploadEndpoints")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Upload(string storageKey, CancellationToken cancellationToken)
    {
        storageKey = Uri.UnescapeDataString(storageKey);
        var contentType = Request.ContentType ?? "application/octet-stream";
        await using var buffer = new MemoryStream();
        await Request.Body.CopyToAsync(buffer, cancellationToken);
        buffer.Position = 0;

        await _virusScanner.ScanAsync(buffer, Path.GetFileName(storageKey), contentType, cancellationToken);
        buffer.Position = 0;
        await _fileStorage.WriteExactAsync(storageKey, buffer, contentType, cancellationToken);
        return NoContent();
    }
}
