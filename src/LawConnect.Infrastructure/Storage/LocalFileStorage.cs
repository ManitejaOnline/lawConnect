using LawConnect.Shared.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using LawConnect.Application.Common.Interfaces;
using LawConnect.Domain.Exceptions;

namespace LawConnect.Infrastructure.Storage;

/// <summary>
/// Local filesystem implementation of <see cref="IFileStorage"/>.
/// </summary>
public sealed class LocalFileStorage : IFileStorage
{
    private readonly StorageOptions _options;
    private readonly IHostEnvironment _environment;
    private readonly LocalStorageUrlSigner _signer;
    private readonly ILogger<LocalFileStorage> _logger;
    private readonly string _rootPath;

    /// <summary>
    /// Initializes a new instance of the <see cref="LocalFileStorage"/> class.
    /// </summary>
    public LocalFileStorage(
        IOptions<StorageOptions> options,
        IHostEnvironment environment,
        LocalStorageUrlSigner signer,
        ILogger<LocalFileStorage> logger)
    {
        _options = options.Value;
        _environment = environment;
        _signer = signer;
        _logger = logger;
        var root = Path.IsPathRooted(_options.RootPath)
            ? _options.RootPath
            : Path.Combine(_environment.ContentRootPath, _options.RootPath);
        _rootPath = Path.TrimEndingDirectorySeparator(Path.GetFullPath(root)) + Path.DirectorySeparatorChar;

        Directory.CreateDirectory(_rootPath);
    }

    /// <inheritdoc />
    public async Task<string> UploadAsync(
        Stream content,
        string fileName,
        string contentType,
        string folder,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);

        var safeFolder = StoragePath.SanitizeFolder(folder);
        var extension = Path.GetExtension(fileName);
        var storageKey = $"{safeFolder}/{Guid.NewGuid():N}{extension}".Replace('\\', '/');
        var fullPath = GetFullPath(storageKey);

        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        await using var fileStream = File.Create(fullPath);
        await content.CopyToAsync(fileStream, cancellationToken);

        _logger.LogInformation("Uploaded file {StorageKey} ({ContentType})", storageKey, contentType);
        return storageKey;
    }

    /// <inheritdoc />
    public Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var fullPath = GetFullPath(storageKey);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
            _logger.LogInformation("Deleted file {StorageKey}", storageKey);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<Uri> GetPresignedUrlAsync(
        string storageKey,
        TimeSpan validFor,
        CancellationToken cancellationToken = default)
    {
        if (StoragePath.IsPrivateKey(storageKey))
        {
            throw new InvalidOperationException("Private documents cannot be exposed as public URLs.");
        }

        if (StoragePath.RequiresSignedAccess(storageKey))
        {
            return Task.FromResult(_signer.CreateDownloadUrl(storageKey, DateTimeOffset.UtcNow.Add(validFor)));
        }

        var baseUrl = _options.PublicBaseUrl.TrimEnd('/');
        var relative = $"{baseUrl}/{storageKey.TrimStart('/')}";
        return Task.FromResult(new Uri(relative, UriKind.RelativeOrAbsolute));
    }

    /// <inheritdoc />
    public Task<PresignedUploadResult> CreatePresignedUploadAsync(
        string fileName,
        string contentType,
        string folder,
        TimeSpan validFor,
        CancellationToken cancellationToken = default)
    {
        var safeFolder = StoragePath.SanitizeFolder(folder);
        var extension = Path.GetExtension(fileName);
        var storageKey = $"{safeFolder}/{Guid.NewGuid():N}{extension}".Replace('\\', '/');
        var expires = DateTime.UtcNow.Add(validFor);
        // Local mode: client uploads through the API upload endpoint (credentials never exposed).
        var uploadUrl = _signer.CreateUploadUrl(
            storageKey,
            contentType,
            _options.MaxUploadBytes,
            new DateTimeOffset(expires, TimeSpan.Zero));
        return Task.FromResult(new PresignedUploadResult(storageKey, uploadUrl, expires));
    }

    /// <inheritdoc />
    public async Task WriteExactAsync(
        string storageKey,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        var fullPath = GetFullPath(storageKey);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        // Signed upload keys are single-use: never overwrite an existing object.
        FileStream fileStream;
        try
        {
            fileStream = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        }
        catch (IOException) when (File.Exists(fullPath))
        {
            throw new ConflictException("A file already exists for this upload key.");
        }

        await using (fileStream)
        {
            await content.CopyToAsync(fileStream, cancellationToken);
        }

        _logger.LogInformation("Wrote exact key {StorageKey} ({ContentType})", storageKey, contentType);
    }

    /// <inheritdoc />
    public Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        var fullPath = GetFullPath(storageKey);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException("Stored file was not found.", storageKey);
        }

        Stream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return Task.FromResult(stream);
    }

    private string GetFullPath(string storageKey)
    {
        var normalized = storageKey.Replace('\\', '/').TrimStart('/');
        var fullPath = Path.GetFullPath(Path.Combine(_rootPath, normalized));
        if (!fullPath.StartsWith(_rootPath, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Invalid storage key.");
        }

        return fullPath;
    }
}
