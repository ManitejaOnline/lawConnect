using System.Security.Cryptography;
using LawConnect.Application.Common.Interfaces;
using LawConnect.Shared.Options;
using Microsoft.Extensions.Options;

namespace LawConnect.Infrastructure.Auth;

/// <summary>
/// Cryptographically secure OTP generator.
/// </summary>
public sealed class OtpGenerator : IOtpGenerator
{
    private readonly OtpOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="OtpGenerator"/> class.
    /// </summary>
    public OtpGenerator(IOptions<OtpOptions> options)
    {
        _options = options.Value;
    }

    /// <inheritdoc />
    public string GenerateNumericCode()
    {
        var max = (int)Math.Pow(10, _options.CodeLength);
        var value = RandomNumberGenerator.GetInt32(0, max);
        return value.ToString($"D{_options.CodeLength}");
    }
}
