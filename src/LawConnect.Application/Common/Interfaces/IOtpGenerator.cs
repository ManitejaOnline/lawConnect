namespace LawConnect.Application.Common.Interfaces;

/// <summary>
/// Generates cryptographically secure one-time passwords.
/// </summary>
public interface IOtpGenerator
{
    /// <summary>
    /// Generates a numeric OTP of the configured length.
    /// </summary>
    /// <returns>Plaintext OTP code.</returns>
    string GenerateNumericCode();
}
