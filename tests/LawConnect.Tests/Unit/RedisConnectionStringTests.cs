using LawConnect.Shared.Infrastructure;

namespace LawConnect.Tests.Unit;

public sealed class RedisConnectionStringTests
{
    [Fact]
    public void Normalize_LeavesHostPortUnchanged()
    {
        Assert.Equal("red-abc:6379", RedisConnectionString.Normalize("red-abc:6379"));
    }

    [Fact]
    public void Normalize_ConvertsRedisUrl()
    {
        var result = RedisConnectionString.Normalize("redis://red-abc:6379");
        Assert.Equal("red-abc:6379,abortConnect=false", result);
    }

    [Fact]
    public void Normalize_ConvertsRedissUrlWithPassword()
    {
        var result = RedisConnectionString.Normalize("rediss://:s3cret@red-abc:6379");
        Assert.Contains("ssl=true", result);
        Assert.Contains("password=s3cret", result);
        Assert.StartsWith("red-abc:6379", result);
    }
}
