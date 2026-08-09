using LawConnect.Application.Common.Models;

namespace LawConnect.Tests.Unit;

public sealed class ResultTests
{
    [Fact]
    public void Success_ContainsValue()
    {
        var result = Result.Success(42);

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
        Assert.Equal(Error.None, result.Error);
    }

    [Fact]
    public void Failure_ThrowsWhenAccessingValue()
    {
        var result = Result.Failure<int>(Error.NotFound("missing"));

        Assert.True(result.IsFailure);
        Assert.Throws<InvalidOperationException>(() => _ = result.Value);
    }
}
