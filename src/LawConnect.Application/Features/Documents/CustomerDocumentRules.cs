#pragma warning disable CS1591
namespace LawConnect.Application.Features.Documents;

/// <summary>Limits for phone-to-phone document transfers. Bytes are never stored on the server.</summary>
public static class CustomerDocumentRules
{
    public const long MaxBytes = 10 * 1024 * 1024;

    public static readonly TimeSpan PendingLifetime = TimeSpan.FromMinutes(10);

    public static readonly TimeSpan ApprovedLifetime = TimeSpan.FromMinutes(2);

    public static readonly TimeSpan RelayWait = TimeSpan.FromSeconds(45);

    public static readonly string[] AllowedContentTypes =
    [
        "application/pdf",
        "image/jpeg",
        "image/png",
        "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
    ];

    public static bool IsAllowedContentType(string? contentType) =>
        AllowedContentTypes.Contains(contentType?.Trim() ?? string.Empty, StringComparer.OrdinalIgnoreCase);

    public static string SanitizeFileName(string fileName)
    {
        var name = Path.GetFileName(fileName.Trim().Replace('\\', '/'));
        if (string.IsNullOrWhiteSpace(name) || name is "." or "..")
        {
            throw new Domain.Exceptions.BusinessRuleException("A file name is required.", "Document.InvalidFileName");
        }

        var cleaned = string.Join("_", name.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
        if (cleaned.Length > 200)
        {
            cleaned = cleaned[..200];
        }

        return cleaned;
    }
}
