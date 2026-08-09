using LawConnect.Infrastructure.Auth;
using LawConnect.Shared.Options;
using Microsoft.Extensions.Options;

namespace LawConnect.Tests.Unit.Auth;

public sealed class OtpGeneratorTests
{
    [Fact]
    public void GenerateNumericCode_RespectsConfiguredLength()
    {
        var generator = new OtpGenerator(Options.Create(new OtpOptions
        {
            HashSecret = "UnitTest_ReplaceWithAtLeast32CharacterSecret!",
            CodeLength = 6
        }));

        var code = generator.GenerateNumericCode();

        Assert.Equal(6, code.Length);
        Assert.True(code.All(char.IsDigit));
    }
}
