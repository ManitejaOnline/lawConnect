using LawConnect.Shared.Infrastructure;

namespace LawConnect.Tests.Unit;

public sealed class PostgresConnectionStringTests
{
    [Fact]
    public void Normalize_LeavesHostKeywordStringUnchanged()
    {
        const string host = "Host=localhost;Port=5432;Database=lawconnect;Username=lawconnect;Password=secret";
        Assert.Equal(host, PostgresConnectionString.Normalize(host));
    }

    [Fact]
    public void Normalize_ConvertsPostgresUrl()
    {
        var result = PostgresConnectionString.Normalize(
            "postgres://lawconnect:p%40ss@dpg-abc-a/lawconnect");

        Assert.Contains("Host=dpg-abc-a", result);
        Assert.Contains("Port=5432", result);
        Assert.Contains("Database=lawconnect", result);
        Assert.Contains("Username=lawconnect", result);
        Assert.Contains("Password=p@ss", result);
        Assert.Contains("SSL Mode=Require", result);
        Assert.Contains("Trust Server Certificate=true", result);
    }

    [Fact]
    public void Normalize_HonorsSslModeQuery()
    {
        var result = PostgresConnectionString.Normalize(
            "postgresql://u:p@db.internal:5432/lawconnect?sslmode=prefer");

        Assert.Contains("SSL Mode=Prefer", result);
        Assert.Contains("Port=5432", result);
    }
}
