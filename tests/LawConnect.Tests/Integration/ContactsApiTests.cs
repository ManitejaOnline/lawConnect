using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using LawConnect.Domain.Enums;
using LawConnect.Infrastructure.Persistence;
using LawConnect.Shared.Constants;
using Microsoft.Extensions.DependencyInjection;

namespace LawConnect.Tests.Integration;

public sealed class ContactsApiTests : IClassFixture<LawConnectWebApplicationFactory>
{
    private readonly LawConnectWebApplicationFactory _factory;

    public ContactsApiTests(LawConnectWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task GetContact_Unauthorized_Returns401()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync($"/api/contacts/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetContact_AuthenticatedCustomer_CanCallLawyer_PublicProfileOmitsPhone()
    {
        var (customer, _) = await _factory.CreateAuthenticatedClientAsync("+919730000001");
        var (lawyerClient, lawyerUserId) = await CreateEligibleLawyerAsync("+919730000002", "BAR-CONTACT-1");

        var lawyerId = await LawyerIdAsync(lawyerClient);
        var profile = await customer.GetAsync($"/api/lawyers/{lawyerId}");
        Assert.True(profile.IsSuccessStatusCode);
        var profileBody = await profile.Content.ReadAsStringAsync();
        Assert.DoesNotContain("+919730000002", profileBody, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("phoneNumber", profileBody, StringComparison.OrdinalIgnoreCase);

        var search = await customer.GetAsync("/api/lawyers/search?pageNumber=1&pageSize=20");
        Assert.True(search.IsSuccessStatusCode);
        var searchBody = await search.Content.ReadAsStringAsync();
        Assert.DoesNotContain("+919730000002", searchBody, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("phoneNumber", searchBody, StringComparison.OrdinalIgnoreCase);

        var contact = await customer.GetFromJsonAsync<CallContactDto>(
            $"/api/contacts/{lawyerUserId}",
            LawConnectWebApplicationFactory.JsonOptions);
        Assert.NotNull(contact);
        Assert.Equal("+919730000002", contact.PhoneNumber);
        Assert.Equal(lawyerUserId, contact.UserId);
    }

    [Fact]
    public async Task GetContact_AssignedSos_BothPartiesReceiveCounterpartPhone()
    {
        var (customerClient, customerLogin) = await _factory.CreateAuthenticatedClientAsync("+919730000011");
        var (lawyerClient, lawyerUserId) = await CreateEligibleLawyerAsync("+919730000012", "BAR-CONTACT-2");
        await CreateAcceptedSosAsync(customerClient, lawyerClient);

        var customerView = await customerClient.GetFromJsonAsync<CallContactDto>(
            $"/api/contacts/{lawyerUserId}",
            LawConnectWebApplicationFactory.JsonOptions);
        Assert.NotNull(customerView);
        Assert.Equal("+919730000012", customerView.PhoneNumber);
        Assert.Equal(lawyerUserId, customerView.UserId);

        var lawyerView = await lawyerClient.GetFromJsonAsync<CallContactDto>(
            $"/api/contacts/{customerLogin.UserId}",
            LawConnectWebApplicationFactory.JsonOptions);
        Assert.NotNull(lawyerView);
        Assert.Equal("+919730000011", lawyerView.PhoneNumber);
        Assert.Equal(customerLogin.UserId, lawyerView.UserId);
    }

    [Fact]
    public async Task GetContact_Stranger_CannotReadAssignedSosPhones()
    {
        var (customerClient, customerLogin) = await _factory.CreateAuthenticatedClientAsync("+919730000021");
        var (lawyerClient, lawyerUserId) = await CreateEligibleLawyerAsync("+919730000022", "BAR-CONTACT-3");
        var (stranger, _) = await _factory.CreateAuthenticatedClientAsync("+919730000023");
        await CreateAcceptedSosAsync(customerClient, lawyerClient);

        var toLawyer = await stranger.GetFromJsonAsync<CallContactDto>(
            $"/api/contacts/{lawyerUserId}",
            LawConnectWebApplicationFactory.JsonOptions);
        Assert.NotNull(toLawyer);
        Assert.Equal("+919730000022", toLawyer.PhoneNumber);

        var toCustomer = await stranger.GetAsync($"/api/contacts/{customerLogin.UserId}");
        Assert.Equal(HttpStatusCode.NotFound, toCustomer.StatusCode);
    }

    [Fact]
    public async Task GetContact_LawyerCannotReadUnrelatedCustomer()
    {
        var (_, customerLogin) = await _factory.CreateAuthenticatedClientAsync("+919730000031");
        var (lawyerClient, _) = await CreateEligibleLawyerAsync("+919730000032", "BAR-CONTACT-4");

        var response = await lawyerClient.GetAsync($"/api/contacts/{customerLogin.UserId}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains("Phone number unavailable", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task CallConnectedEndpoint_IsRemoved()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync($"/api/emergency/sos/{Guid.NewGuid()}/call-connected", new { });
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CallSessionsEndpoint_IsRemoved()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/calls/sessions", new { callType = "voice" });
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task<(HttpClient Client, Guid UserId)> CreateEligibleLawyerAsync(string phone, string bar)
    {
        var (client, login) = await _factory.CreateAuthenticatedClientAsync(phone);
        var register = await client.PostAsJsonAsync(
            "/api/lawyers/register",
            new
            {
                barCouncilNumber = bar,
                bio = "Contact lawyer",
                yearsOfExperience = 6,
                consultationFee = 1000m,
                emergencyFee = 2000m,
                specializationIds = new[] { SeedIds.Specializations.CriminalLaw }
            });
        Assert.True(register.IsSuccessStatusCode, await register.Content.ReadAsStringAsync());
        var profile = await register.Content.ReadFromJsonAsync<LawyerProfileMiniDto>(
            LawConnectWebApplicationFactory.JsonOptions);
        Assert.NotNull(profile);

        var address = await client.PostAsJsonAsync(
            "/api/users/me/addresses",
            new
            {
                line1 = "Contact Address",
                line2 = (string?)null,
                postalCode = "560001",
                landmark = (string?)null,
                cityId = (Guid?)null,
                latitude = 12.97,
                longitude = 77.59,
                isPrimary = true,
                label = "Office"
            });
        Assert.True(address.IsSuccessStatusCode, await address.Content.ReadAsStringAsync());

        await client.PutAsJsonAsync("/api/lawyers/me/online-status", new { isOnline = true });
        await client.PutAsJsonAsync(
            "/api/lawyers/me/emergency-availability",
            new { isAvailableForEmergency = true, emergencyFee = 2000m });

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var lawyer = await db.Lawyers.FindAsync(profile.LawyerId);
        Assert.NotNull(lawyer);
        lawyer.IsVerified = true;
        lawyer.VerificationStatus = VerificationStatus.Approved;
        lawyer.IsOnline = true;
        lawyer.IsAvailableForEmergency = true;
        lawyer.RatingAverage = 4.5;
        lawyer.RatingCount = 20;
        await db.SaveChangesAsync();

        var (reauthed, reauthLogin) = await _factory.CreateAuthenticatedClientAsync(phone);
        return (reauthed, reauthLogin.UserId);
    }

    private static async Task<Guid> LawyerIdAsync(HttpClient lawyerClient)
    {
        var mine = await lawyerClient.GetFromJsonAsync<JsonElement>(
            "/api/lawyers/me",
            LawConnectWebApplicationFactory.JsonOptions);
        return mine.GetProperty("lawyerId").GetGuid();
    }

    private static async Task CreateAcceptedSosAsync(HttpClient customer, HttpClient lawyer)
    {
        var create = await customer.PostAsJsonAsync(
            "/api/emergency/sos",
            new
            {
                categoryId = SeedIds.EmergencyCategories.CriminalEmergency,
                latitude = 12.97,
                longitude = 77.59,
                notes = "Contact test"
            });
        Assert.True(create.IsSuccessStatusCode, await create.Content.ReadAsStringAsync());

        var incoming = await lawyer.GetFromJsonAsync<IncomingSosDto[]>(
            "/api/emergency/incoming",
            LawConnectWebApplicationFactory.JsonOptions);
        Assert.NotNull(incoming);
        Assert.NotEmpty(incoming);

        var accept = await lawyer.PostAsJsonAsync(
            $"/api/emergency/assignments/{incoming[0].AssignmentId}/accept",
            new { });
        Assert.True(accept.IsSuccessStatusCode, await accept.Content.ReadAsStringAsync());
    }

    private sealed record CallContactDto(Guid UserId, string DisplayName, string PhoneNumber);

    private sealed record IncomingSosDto(Guid RequestId, Guid AssignmentId);

    private sealed record LawyerProfileMiniDto(Guid LawyerId);
}
