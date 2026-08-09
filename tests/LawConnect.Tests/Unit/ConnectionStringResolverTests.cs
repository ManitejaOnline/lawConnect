using LawConnect.Api.Configuration;
using LawConnect.Shared.Infrastructure;
using Microsoft.Extensions.Configuration;

namespace LawConnect.Tests.Unit;

public sealed class ConnectionStringResolverTests
{
    [Fact]
    public void IsLoopback_DetectsLocalhostHostKeyword()
    {
        Assert.True(ConnectionStringResolver.IsLoopback(
            "Host=localhost;Port=5432;Database=lawconnect;Username=lawconnect;Password=secret"));
    }

    [Fact]
    public void IsLoopback_DetectsLoopbackIp()
    {
        Assert.True(ConnectionStringResolver.IsLoopback("Host=127.0.0.1;Port=5432;Database=lawconnect"));
    }

    [Fact]
    public void IsLoopback_AllowsRenderInternalHost()
    {
        Assert.False(ConnectionStringResolver.IsLoopback(
            "Host=dpg-abc-a;Port=5432;Database=lawconnect;Username=lawconnect;Password=secret"));
    }

    [Fact]
    public void Coalesce_PrefersNonLoopbackOverLocalhostHangfire()
    {
        var selected = ConnectionStringResolver.Coalesce(
            "Host=localhost;Port=5432;Database=lawconnect",
            "Host=dpg-abc-a;Port=5432;Database=lawconnect");

        Assert.Equal("Host=dpg-abc-a;Port=5432;Database=lawconnect", selected);
    }

    [Fact]
    public void Fallbacks_CopyDefaultConnectionOntoEmptyHangfire()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Host=dpg-abc-a;Database=lawconnect",
                ["ConnectionStrings:HangfireConnection"] = "",
            })
            .AddProductionConnectionFallbacks()
            .Build();

        Assert.Equal(
            "Host=dpg-abc-a;Database=lawconnect",
            configuration.GetConnectionString("HangfireConnection"));
    }

    [Fact]
    public void Fallbacks_ReplaceLocalhostHangfireWithDefaultConnection()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Host=dpg-abc-a;Database=lawconnect",
                ["ConnectionStrings:HangfireConnection"] =
                    "Host=localhost;Port=5432;Database=lawconnect;Username=lawconnect;Password=secret",
            })
            .AddProductionConnectionFallbacks()
            .Build();

        Assert.Equal(
            "Host=dpg-abc-a;Database=lawconnect",
            configuration.GetConnectionString("HangfireConnection"));
    }

    [Fact]
    public void Fallbacks_MapDatabaseUrlWhenDefaultConnectionIsLoopback()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Port=5432;Database=lawconnect",
                ["DATABASE_URL"] = "postgres://u:p@dpg-abc-a/lawconnect",
            })
            .AddProductionConnectionFallbacks()
            .Build();

        Assert.Equal(
            "postgres://u:p@dpg-abc-a/lawconnect",
            configuration.GetConnectionString("DefaultConnection"));
        Assert.Equal(
            "postgres://u:p@dpg-abc-a/lawconnect",
            configuration.GetConnectionString("HangfireConnection"));
    }

    [Fact]
    public void EnsureNotLoopbackInProduction_ThrowsWithoutExposingSecret()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            ConnectionStringResolver.EnsureNotLoopbackInProduction(
                "ConnectionStrings__DefaultConnection",
                "Host=localhost;Password=super-secret-value",
                isProduction: true));

        Assert.Contains("ConnectionStrings__DefaultConnection", ex.Message);
        Assert.DoesNotContain("super-secret-value", ex.Message);
    }
}
