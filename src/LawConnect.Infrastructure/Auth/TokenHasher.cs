using System.Security.Cryptography;
using System.Text;
using LawConnect.Application.Common.Interfaces;
using LawConnect.Shared.Options;
using Microsoft.Extensions.Options;

namespace LawConnect.Infrastructure.Auth;

/// <summary>
/// Generates opaque tokens and HMAC hashes for OTP storage.
/// </summary>
public sealed class TokenHasher : ITokenHasher
{
    private readonly byte[] _key;

    /// <summary>
    /// Initializes a new instance of the <see cref="TokenHasher"/> class.
    /// </summary>
    public TokenHasher(IOptions<OtpOptions> otpOptions)
    {
        _key = Encoding.UTF8.GetBytes(otpOptions.Value.HashSecret);
    }

    /// <inheritdoc />
    public string GenerateOpaqueToken()
    {
        Span<byte> bytes = stackalloc byte[64];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    /// <inheritdoc />
    public string Hash(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        using var hmac = new HMACSHA256(_key);
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(hash);
    }

    /// <inheritdoc />
    public bool Verify(string value, string hash)
    {
        if (string.IsNullOrWhiteSpace(value) || string.IsNullOrWhiteSpace(hash))
        {
            return false;
        }

        var computed = Hash(value);
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(computed),
            Encoding.UTF8.GetBytes(hash));
    }
}
