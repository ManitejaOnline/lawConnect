using LawConnect.Infrastructure.Auth;
using LawConnect.Shared.Options;
using Microsoft.Extensions.Options;

namespace LawConnect.Tests.Unit.Auth;

public sealed class TokenHasherTests
{
    [Fact]
    public void Hash_And_Verify_RoundTrip()
    {
        var hasher = CreateHasher();
        var hash = hasher.Hash("123456");

        Assert.True(hasher.Verify("123456", hash));
        Assert.False(hasher.Verify("000000", hash));
    }

    [Fact]
    public void GenerateOpaqueToken_IsUnique()
    {
        var hasher = CreateHasher();
        var first = hasher.GenerateOpaqueToken();
        var second = hasher.GenerateOpaqueToken();

        Assert.NotEqual(first, second);
        Assert.True(first.Length >= 32);
    }

    private static TokenHasher CreateHasher()
    {
        var options = Options.Create(new OtpOptions
        {
            HashSecret = "UnitTest_ReplaceWithAtLeast32CharacterSecret!"
        });

        return new TokenHasher(options);
    }
}
