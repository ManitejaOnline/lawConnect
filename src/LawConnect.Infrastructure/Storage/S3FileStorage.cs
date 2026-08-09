using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using LawConnect.Application.Common.Interfaces;
using LawConnect.Shared.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LawConnect.Infrastructure.Storage;

/// <summary>
/// AWS S3 implementation of <see cref="IFileStorage"/> using signed URLs (credentials never exposed to clients).
/// </summary>
public sealed class S3FileStorage : IFileStorage, IDisposable
{
    private readonly AwsS3Options _options;
    private readonly IAmazonS3 _client;
    private readonly ILogger<S3FileStorage> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="S3FileStorage"/> class.
    /// </summary>
    public S3FileStorage(IOptions<AwsS3Options> options, ILogger<S3FileStorage> logger)
    {
        _options = options.Value;
        _logger = logger;

        var region = RegionEndpoint.GetBySystemName(_options.Region);
        if (!string.IsNullOrWhiteSpace(_options.AccessKey) && !string.IsNullOrWhiteSpace(_options.SecretKey))
        {
            _client = new AmazonS3Client(
                new BasicAWSCredentials(_options.AccessKey, _options.SecretKey),
                region);
        }
        else
        {
            _client = new AmazonS3Client(region);
        }
    }

    /// <inheritdoc />
    public async Task<string> UploadAsync(
        Stream content,
        string fileName,
        string contentType,
        string folder,
        CancellationToken cancellationToken = default)
    {
        var storageKey = BuildKey(folder, fileName);
        var request = new PutObjectRequest
        {
            BucketName = _options.BucketName,
            Key = storageKey,
            InputStream = content,
            ContentType = contentType,
            AutoCloseStream = false
        };

        await _client.PutObjectAsync(request, cancellationToken);
        _logger.LogInformation("Uploaded S3 object {StorageKey}", storageKey);
        return storageKey;
    }

    /// <inheritdoc />
    public async Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        await _client.DeleteObjectAsync(_options.BucketName, storageKey, cancellationToken);
        _logger.LogInformation("Deleted S3 object {StorageKey}", storageKey);
    }

    /// <inheritdoc />
    public Task<Uri> GetPresignedUrlAsync(
        string storageKey,
        TimeSpan validFor,
        CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        if (StoragePath.IsPrivateKey(storageKey))
        {
            throw new InvalidOperationException("Private documents cannot be exposed as public URLs.");
        }

        var url = _client.GetPreSignedURL(new GetPreSignedUrlRequest
        {
            BucketName = _options.BucketName,
            Key = storageKey,
            Verb = HttpVerb.GET,
            Expires = DateTime.UtcNow.Add(validFor)
        });
        return Task.FromResult(new Uri(url));
    }

    /// <inheritdoc />
    public Task<PresignedUploadResult> CreatePresignedUploadAsync(
        string fileName,
        string contentType,
        string folder,
        TimeSpan validFor,
        CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        var storageKey = BuildKey(folder, fileName);
        var expires = DateTime.UtcNow.Add(validFor);
        var url = _client.GetPreSignedURL(new GetPreSignedUrlRequest
        {
            BucketName = _options.BucketName,
            Key = storageKey,
            Verb = HttpVerb.PUT,
            Expires = expires,
            ContentType = contentType
        });

        return Task.FromResult(new PresignedUploadResult(storageKey, new Uri(url), expires));
    }

    /// <inheritdoc />
    public async Task WriteExactAsync(
        string storageKey,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        var request = new PutObjectRequest
        {
            BucketName = _options.BucketName,
            Key = storageKey,
            InputStream = content,
            ContentType = contentType,
            AutoCloseStream = false
        };
        await _client.PutObjectAsync(request, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var response = await _client.GetObjectAsync(_options.BucketName, storageKey, cancellationToken);
        return response.ResponseStream;
    }

    /// <inheritdoc />
    public void Dispose() => _client.Dispose();

    private static string BuildKey(string folder, string fileName)
    {
        var safeFolder = StoragePath.SanitizeFolder(folder);
        var extension = Path.GetExtension(fileName);
        return $"{safeFolder}/{Guid.NewGuid():N}{extension}".Replace('\\', '/');
    }
}
