namespace LawConnect.Application.Common.Interfaces;

/// <summary>
/// Scans uploaded content for malware before accepting attachments.
/// </summary>
public interface IVirusScanner
{
    /// <summary>
    /// Scans a stream and throws when malware is detected.
    /// </summary>
    /// <param name="content">Content stream.</param>
    /// <param name="fileName">Original file name.</param>
    /// <param name="contentType">MIME type.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ScanAsync(
        Stream content,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default);
}
