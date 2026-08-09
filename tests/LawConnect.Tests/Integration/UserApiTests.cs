using System.Net.Http.Json;

namespace LawConnect.Tests.Integration;

[Collection("Integration")]
public sealed class UserApiTests
{
    private readonly LawConnectWebApplicationFactory _factory;

    public UserApiTests(LawConnectWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ProfileAddressAndNotificationSettings_Flow_Succeeds()
    {
        var (client, _) = await _factory.CreateAuthenticatedClientAsync("+919811122233");

        var updateProfile = await client.PutAsJsonAsync(
            "/api/users/me",
            new
            {
                firstName = "Asha",
                lastName = "Patel",
                email = "asha.patel@example.com",
                dateOfBirth = "1990-05-01",
                gender = "Female",
                preferredLanguage = "en"
            });
        Assert.True(updateProfile.IsSuccessStatusCode, await updateProfile.Content.ReadAsStringAsync());

        var profile = await client.GetFromJsonAsync<UserProfileDto>(
            "/api/users/me",
            LawConnectWebApplicationFactory.JsonOptions);
        Assert.NotNull(profile);
        Assert.Equal("Asha", profile.FirstName);
        Assert.Equal("asha.patel@example.com", profile.Email);

        var createAddress = await client.PostAsJsonAsync(
            "/api/users/me/addresses",
            new
            {
                line1 = "12 MG Road",
                line2 = "Near Metro",
                postalCode = "560001",
                landmark = "Park",
                cityId = (Guid?)null,
                latitude = 12.97,
                longitude = 77.59,
                isPrimary = true,
                label = "Home"
            });
        Assert.True(createAddress.IsSuccessStatusCode, await createAddress.Content.ReadAsStringAsync());

        var addresses = await client.GetFromJsonAsync<AddressDto[]>(
            "/api/users/me/addresses",
            LawConnectWebApplicationFactory.JsonOptions);
        Assert.NotNull(addresses);
        Assert.Single(addresses);
        Assert.True(addresses[0].IsPrimary);

        var updateSettings = await client.PutAsJsonAsync(
            "/api/users/me/notification-settings",
            new
            {
                enablePush = true,
                enableSms = false,
                enableEmail = true,
                enableInApp = true
            });
        Assert.True(updateSettings.IsSuccessStatusCode, await updateSettings.Content.ReadAsStringAsync());

        var settings = await client.GetFromJsonAsync<NotificationSettingsDto>(
            "/api/users/me/notification-settings",
            LawConnectWebApplicationFactory.JsonOptions);
        Assert.NotNull(settings);
        Assert.False(settings.EnableSms);
        Assert.True(settings.EnableEmail);

        var devices = await client.GetFromJsonAsync<DeviceDto[]>(
            "/api/users/me/devices",
            LawConnectWebApplicationFactory.JsonOptions);
        Assert.NotNull(devices);
        Assert.Contains(devices, d => d.DeviceIdentifier == "test-device-1");
    }

    private sealed record UserProfileDto(
        Guid UserId,
        string PhoneNumber,
        string? Email,
        string FirstName,
        string LastName);

    private sealed record AddressDto(Guid Id, string Line1, bool IsPrimary);

    private sealed record NotificationSettingsDto(bool EnablePush, bool EnableSms, bool EnableEmail, bool EnableInApp);

    private sealed record DeviceDto(Guid Id, string DeviceIdentifier, bool IsActive);
}
