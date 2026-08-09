namespace LawConnect.Infrastructure.Storage;

/// <summary>
/// Shared storage key helpers. Private verification documents must keep a <c>private/</c> prefix.
/// </summary>
public static class StoragePath
{
    /// <summary>
    /// Sanitizes folder segments while preserving directory separators.
    /// </summary>
    /// <param name="folder">Logical folder, for example <c>private/lawyers/{id}/documents</c>.</param>
    /// <returns>Safe relative folder path.</returns>
    public static string SanitizeFolder(string folder)
    {
        var parts = folder.Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var cleaned = new List<string>();
        foreach (var part in parts)
        {
            var safe = string.Join("-", part.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
            if (!string.IsNullOrWhiteSpace(safe))
            {
                cleaned.Add(safe);
            }
        }

        return cleaned.Count == 0 ? "misc" : string.Join('/', cleaned);
    }

    /// <summary>
    /// Returns whether a storage key must never be served as a public URL.
    /// </summary>
    /// <param name="storageKey">Object key or public request path.</param>
    /// <returns><c>true</c> when the key is private.</returns>
    public static bool IsPrivateKey(string storageKey)
    {
        var normalized = storageKey.Replace('\\', '/').TrimStart('/');
        return normalized.StartsWith("private/", StringComparison.OrdinalIgnoreCase)
            || normalized.StartsWith("private-", StringComparison.OrdinalIgnoreCase);
    }
}
