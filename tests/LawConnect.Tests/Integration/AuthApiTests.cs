using System.Net;
using System.Net.Http.Json;
using LawConnect.Domain.Enums;
using LawConnect.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LawConnect.Tests.Integration;

[Collection("Integration")]
public sealed class AuthApiTests
{
    private readonly LawConnectWebApplicationFactory _factory;

    public AuthApiTests(LawConnectWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task RequestVerifyRefreshLogout_Flow_Succeeds()
    {
        var client = _factory.CreateClient();
        const string phone = "+919876543210";
        _factory.Sms.Reset();

        var requestResponse = await client.PostAsJsonAsync(
            "/api/auth/otp/request",
            new { phoneNumber = phone });

        Assert.True(requestResponse.IsSuccessStatusCode, await requestResponse.Content.ReadAsStringAsync());

        var otp = System.Text.RegularExpressions.Regex.Match(
            _factory.Sms.LastMessage ?? string.Empty,
            @"\b(\d{6})\b").Groups[1].Value;
        Assert.False(string.IsNullOrWhiteSpace(otp));

        var verifyResponse = await client.PostAsJsonAsync(
            "/api/auth/otp/verify",
            new
            {
                phoneNumber = phone,
                code = otp,
                deviceIdentifier = "test-device-1",
                platform = DevicePlatform.Android
            });

        Assert.True(verifyResponse.IsSuccessStatusCode, await verifyResponse.Content.ReadAsStringAsync());
        var login = await verifyResponse.Content.ReadFromJsonAsync<LoginDto>(LawConnectWebApplicationFactory.JsonOptions);
        Assert.NotNull(login);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var stored = await db.RefreshTokens
                .Where(t => t.UserId == login.UserId && t.RevokedAtUtc == null)
                .ToListAsync();
            Assert.NotEmpty(stored);
            Assert.All(stored, token =>
            {
                Assert.NotEqual(login.Tokens.RefreshToken, token.Token);
                Assert.Equal(64, token.Token.Length);
            });
        }

        var refreshResponse = await client.PostAsJsonAsync(
            "/api/auth/refresh",
            new
            {
                refreshToken = login.Tokens.RefreshToken,
                deviceIdentifier = "test-device-1",
                platform = DevicePlatform.Android
            });

        Assert.True(refreshResponse.IsSuccessStatusCode, await refreshResponse.Content.ReadAsStringAsync());
        var refreshed = await refreshResponse.Content.ReadFromJsonAsync<TokenDto>(LawConnectWebApplicationFactory.JsonOptions);
        Assert.NotNull(refreshed);
        Assert.NotEqual(login.Tokens.RefreshToken, refreshed.RefreshToken);

        var logoutResponse = await client.PostAsJsonAsync(
            "/api/auth/logout",
            new { refreshToken = refreshed.RefreshToken });

        Assert.Equal(HttpStatusCode.NoContent, logoutResponse.StatusCode);
    }

    [Fact]
    public async Task DeleteAccount_RevokesSession_AndRejectsTheOldAccessToken()
    {
        var (client, login) = await _factory.CreateAuthenticatedClientAsync("+919876543211");
        var me = await client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);

        var deleted = await client.DeleteAsync("/api/auth/account");
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);

        var after = await client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, after.StatusCode);

        var refresh = await _factory.CreateClient().PostAsJsonAsync(
            "/api/auth/refresh",
            new { refreshToken = login.Tokens.RefreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);
    }
}
