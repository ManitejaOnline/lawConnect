namespace LawConnect.Application.Common.Interfaces;

/// <summary>
/// Hashes and verifies account passwords. Identity providers (OTP, Google) can coexist without using this.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>
    /// Creates a one-way hash for a plaintext password.
    /// </summary>
    /// <param name="password">Plaintext password.</param>
    /// <returns>Stored hash.</returns>
    string Hash(string password);

    /// <summary>
    /// Verifies a plaintext password against a stored hash.
    /// </summary>
    /// <param name="hash">Stored hash.</param>
    /// <param name="password">Plaintext password.</param>
    /// <returns><c>true</c> when the password matches.</returns>
    bool Verify(string hash, string password);
}
