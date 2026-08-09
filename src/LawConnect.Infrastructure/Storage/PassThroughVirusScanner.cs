using LawConnect.Application.Common.Interfaces;
using LawConnect.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace LawConnect.Infrastructure.Storage;

/// <summary>
/// Pass-through virus scanner used until an external AV provider is configured.
/// Rejects empty streams and logs scan attempts.
/// </summary>
public sealed class PassThroughVirusScanner : IVirusScanner
{
    private readonly ILogger<PassThroughVirusScanner> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PassThroughVirusScanner"/> class.
    /// </summary>
    public PassThroughVirusScanner(ILogger<PassThroughVirusScanner> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task ScanAsync(
        Stream content,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        if (content.CanSeek && content.Length <= 0)
        {
            throw new BusinessRuleException("Empty files are not allowed.", "Storage.EmptyFile");
        }

        // Ensure the stream is readable without consuming caller buffers unexpectedly.
        var buffer = new byte[1];
        var read = await content.ReadAsync(buffer.AsMemory(0, 1), cancellationToken);
        if (read == 0)
        {
            throw new BusinessRuleException("Empty files are not allowed.", "Storage.EmptyFile");
        }

        if (content.CanSeek)
        {
            content.Position = 0;
        }

        _logger.LogInformation("Virus scan passed for {FileName} ({ContentType})", fileName, contentType);
    }
}
