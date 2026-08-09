namespace LawConnect.Application.Common.Interfaces;

/// <summary>
/// Generates and hashes opaque security tokens (OTP hashes, refresh tokens).
/// </summary>
public interface ITokenHasher
{
    /// <summary>
    /// Creates a cryptographically secure opaque token string.
    /// </summary>
    /// <returns>URL-safe token.</returns>
    string GenerateOpaqueToken();

    /// <summary>
    /// Hashes a sensitive value for durable storage.
    /// </summary>
    /// <param name="value">Plaintext value.</param>
    /// <returns>Hex-encoded HMAC hash.</returns>
    string Hash(string value);

    /// <summary>
    /// Compares a plaintext value to a stored hash in constant time.
    /// </summary>
    /// <param name="value">Plaintext value.</param>
    /// <param name="hash">Stored hash.</param>
    /// <returns><c>true</c> when the value matches.</returns>
    bool Verify(string value, string hash);
}
