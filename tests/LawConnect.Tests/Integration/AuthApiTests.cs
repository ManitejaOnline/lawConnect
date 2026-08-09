using System.Net.Http.Json;
using LawConnect.Domain.Enums;

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

        Assert.Equal(System.Net.HttpStatusCode.NoContent, logoutResponse.StatusCode);
    }
}
