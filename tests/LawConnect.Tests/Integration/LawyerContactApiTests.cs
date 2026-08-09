using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using LawConnect.Domain.Enums;
using LawConnect.Infrastructure.Persistence;
using LawConnect.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LawConnect.Tests.Integration;

[Collection("Integration")]
public sealed class LawyerContactApiTests
{
    private readonly LawConnectWebApplicationFactory _factory;

    public LawyerContactApiTests(LawConnectWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task GetContact_Unauthorized_Returns401()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync($"/api/lawyers/{Guid.NewGuid()}/contact");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetContact_LawyerRole_Returns403()
    {
        var (lawyerClient, _) = await CreateVerifiedLawyerAsync("+919876543211", "BAR-CONTACT-ROLE");
        var lawyerId = await LawyerIdAsync(lawyerClient);

        var response = await lawyerClient.GetAsync($"/api/lawyers/{lawyerId}/contact");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetContact_Customer_ReturnsPhone_PublicProfileOmitsPhone()
    {
        var (customer, _) = await _factory.CreateAuthenticatedClientAsync("+919876543200");
        var (lawyerClient, _) = await CreateVerifiedLawyerAsync("+919876543201", "BAR-PRIYA-CONTACT");
        var lawyerId = await LawyerIdAsync(lawyerClient);

        var profile = await customer.GetAsync($"/api/lawyers/{lawyerId}");
        Assert.True(profile.IsSuccessStatusCode);
        var profileBody = await profile.Content.ReadAsStringAsync();
        Assert.DoesNotContain("+919876543201", profileBody, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("phoneNumber", profileBody, StringComparison.OrdinalIgnoreCase);

        var search = await customer.GetAsync("/api/lawyers/search?pageNumber=1&pageSize=50");
        Assert.True(search.IsSuccessStatusCode);
        var searchBody = await search.Content.ReadAsStringAsync();
        Assert.DoesNotContain("+919876543201", searchBody, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("phoneNumber", searchBody, StringComparison.OrdinalIgnoreCase);

        var contact = await customer.GetFromJsonAsync<LawyerContactDto>(
            $"/api/lawyers/{lawyerId}/contact",
            LawConnectWebApplicationFactory.JsonOptions);
        Assert.NotNull(contact);
        Assert.Equal("+919876543201", contact.PhoneNumber);
    }

    [Fact]
    public async Task GetContact_UnverifiedLawyer_Returns404()
    {
        var (customer, _) = await _factory.CreateAuthenticatedClientAsync("+919876543210");
        var (lawyerClient, _) = await _factory.CreateAuthenticatedClientAsync("+919876543212");
        var register = await lawyerClient.PostAsJsonAsync(
            "/api/lawyers/register",
            new
            {
                barCouncilNumber = "BAR-UNVERIFIED-CONTACT",
                bio = "Pending verification",
                yearsOfExperience = 3,
                consultationFee = 800m,
                emergencyFee = 1200m,
                specializationIds = new[] { SeedIds.Specializations.CivilLaw }
            });
        Assert.True(register.IsSuccessStatusCode, await register.Content.ReadAsStringAsync());
        var lawyerId = await LawyerIdAsync(lawyerClient);

        var response = await customer.GetAsync($"/api/lawyers/{lawyerId}/contact");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains("Phone number unavailable", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task GetContact_UnknownLawyer_Returns404()
    {
        var (customer, _) = await _factory.CreateAuthenticatedClientAsync("+919876543213");
        var response = await customer.GetAsync($"/api/lawyers/{Guid.NewGuid()}/contact");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetContact_InactiveLawyerUser_Returns404()
    {
        var (customer, _) = await _factory.CreateAuthenticatedClientAsync("+919876543214");
        var (lawyerClient, lawyerUserId) = await CreateVerifiedLawyerAsync("+919876543215", "BAR-INACTIVE-CONTACT");
        var lawyerId = await LawyerIdAsync(lawyerClient);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var user = await db.Users.SingleAsync(u => u.Id == lawyerUserId);
            user.IsActive = false;
            await db.SaveChangesAsync();
        }

        var response = await customer.GetAsync($"/api/lawyers/{lawyerId}/contact");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains("Phone number unavailable", await response.Content.ReadAsStringAsync());
    }

    private async Task<(HttpClient Client, Guid UserId)> CreateVerifiedLawyerAsync(string phone, string bar)
    {
        var (client, login) = await _factory.CreateAuthenticatedClientAsync(phone);
        var register = await client.PostAsJsonAsync(
            "/api/lawyers/register",
            new
            {
                barCouncilNumber = bar,
                bio = "Verified contact lawyer",
                yearsOfExperience = 8,
                consultationFee = 1500m,
                emergencyFee = 2500m,
                specializationIds = new[] { SeedIds.Specializations.CriminalLaw }
            });
        Assert.True(register.IsSuccessStatusCode, await register.Content.ReadAsStringAsync());
        var mini = await register.Content.ReadFromJsonAsync<LawyerProfileMiniDto>(
            LawConnectWebApplicationFactory.JsonOptions);
        Assert.NotNull(mini);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var lawyer = await db.Lawyers.FindAsync(mini.LawyerId);
        Assert.NotNull(lawyer);
        lawyer.IsVerified = true;
        lawyer.VerificationStatus = VerificationStatus.Approved;
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

    private sealed record LawyerContactDto(string PhoneNumber);

    private sealed record LawyerProfileMiniDto(Guid LawyerId);
}
