using LawConnect.Application.Common.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace LawConnect.Infrastructure.Auth;

/// <summary>
/// ASP.NET Identity password hasher wrapper.
/// </summary>
public sealed class AspNetPasswordHasher : IPasswordHasher
{
    private readonly PasswordHasher<object> _hasher = new();

    /// <inheritdoc />
    public string Hash(string password) => _hasher.HashPassword(new object(), password);

    /// <inheritdoc />
    public bool Verify(string hash, string password)
    {
        if (string.IsNullOrWhiteSpace(hash) || string.IsNullOrWhiteSpace(password))
        {
            return false;
        }

        var result = _hasher.VerifyHashedPassword(new object(), hash, password);
        return result is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
    }
}
