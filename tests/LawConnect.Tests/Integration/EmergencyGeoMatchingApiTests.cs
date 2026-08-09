using System.Net.Http.Json;
using LawConnect.Domain.Enums;
using LawConnect.Infrastructure.Persistence;
using LawConnect.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LawConnect.Tests.Integration;

[Collection("Integration")]
public sealed class EmergencyGeoMatchingApiTests
{
    private readonly LawConnectWebApplicationFactory _factory;

    public EmergencyGeoMatchingApiTests(LawConnectWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task SameCity_IsNotifiedBeforeSameState()
    {
        await _factory.ResetEmergencyStateAsync();
        var (customer, _) = await _factory.CreateAuthenticatedClientAsync(UniquePhone());
        var city = await CreateLawyerAsync(UniquePhone(), UniqueBar("C1"), 12.9716, 77.5946);
        var state = await CreateLawyerAsync(UniquePhone(), UniqueBar("S1"), 12.2958, 76.6394);

        var sos = await CreateSosAsync(customer, 12.9716, 77.5946);
        Assert.Equal(1, sos.AttemptNumber);
        Assert.Equal("City", sos.SearchScope);
        Assert.Equal("Bengaluru", sos.CityName);

        Assert.Contains(await IncomingAsync(city.Client), x => x.RequestId == sos.RequestId);
        Assert.DoesNotContain(await IncomingAsync(state.Client), x => x.RequestId == sos.RequestId);
    }

    [Fact]
    public async Task SameState_IsNotifiedBeforeSameCountry()
    {
        await _factory.ResetEmergencyStateAsync();
        var (customer, _) = await _factory.CreateAuthenticatedClientAsync(UniquePhone());
        var state = await CreateLawyerAsync(UniquePhone(), UniqueBar("ST1"), 12.2958, 76.6394);
        var country = await CreateLawyerAsync(UniquePhone(), UniqueBar("CO1"), 19.0760, 72.8777);

        var sos = await CreateSosAsync(customer, 12.9716, 77.5946);
        Assert.True(sos.AttemptNumber >= 2, sos.AttemptNumber.ToString());
        Assert.Equal("State", sos.SearchScope);

        Assert.Contains(await IncomingAsync(state.Client), x => x.RequestId == sos.RequestId);
        Assert.DoesNotContain(await IncomingAsync(country.Client), x => x.RequestId == sos.RequestId);
    }

    [Fact]
    public async Task SameCountry_IsNotified_InternationalIsNot()
    {
        await _factory.ResetEmergencyStateAsync();
        var (customer, _) = await _factory.CreateAuthenticatedClientAsync(UniquePhone());
        var india = await CreateLawyerAsync(UniquePhone(), UniqueBar("IN1"), 19.0760, 72.8777);
        var uk = await CreateLawyerAsync(UniquePhone(), UniqueBar("UK1"), 51.5074, -0.1278);

        var sos = await CreateSosAsync(customer, 12.9716, 77.5946);
        Assert.True(sos.AttemptNumber >= 3, sos.AttemptNumber.ToString());
        Assert.Equal("Country", sos.SearchScope);

        Assert.Contains(await IncomingAsync(india.Client), x => x.RequestId == sos.RequestId);
        Assert.DoesNotContain(await IncomingAsync(uk.Client), x => x.RequestId == sos.RequestId);
    }

    [Fact]
    public async Task NoCityLawyers_NotifiesState()
    {
        await _factory.ResetEmergencyStateAsync();
        var (customer, _) = await _factory.CreateAuthenticatedClientAsync(UniquePhone());
        var mysuru = await CreateLawyerAsync(UniquePhone(), UniqueBar("MY1"), 12.2958, 76.6394);

        var sos = await CreateSosAsync(customer, 12.9716, 77.5946);
        Assert.Equal("State", sos.SearchScope);
        Assert.Contains(await IncomingAsync(mysuru.Client), x => x.RequestId == sos.RequestId);
    }

    [Fact]
    public async Task NoStateLawyers_NotifiesCountry()
    {
        await _factory.ResetEmergencyStateAsync();
        var (customer, _) = await _factory.CreateAuthenticatedClientAsync(UniquePhone());
        var mumbai = await CreateLawyerAsync(UniquePhone(), UniqueBar("MU1"), 19.0760, 72.8777);

        var sos = await CreateSosAsync(customer, 12.9716, 77.5946);
        Assert.Equal("Country", sos.SearchScope);
        Assert.Contains(await IncomingAsync(mumbai.Client), x => x.RequestId == sos.RequestId);
    }

    [Fact]
    public async Task EarlierGroupLawyer_CanAcceptAfterEscalation()
    {
        await _factory.ResetEmergencyStateAsync();
        var (customer, _) = await _factory.CreateAuthenticatedClientAsync(UniquePhone());
        var city = await CreateLawyerAsync(UniquePhone(), UniqueBar("EA1"), 12.9716, 77.5946);
        var sos = await CreateSosAsync(customer, 12.9716, 77.5946);
        var assignmentId = (await IncomingAsync(city.Client)).First(x => x.RequestId == sos.RequestId).AssignmentId;

        using var scope = _factory.Services.CreateScope();
        var processor = scope.ServiceProvider.GetRequiredService<LawConnect.Application.Common.Interfaces.IEmergencyTimeoutProcessor>();
        await processor.ProcessTimeoutAsync(sos.RequestId, sos.AttemptNumber);

        Assert.Contains(await IncomingAsync(city.Client), x => x.AssignmentId == assignmentId);
        var accept = await city.Client.PostAsJsonAsync($"/api/emergency/assignments/{assignmentId}/accept", new { });
        Assert.True(accept.IsSuccessStatusCode, await accept.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task DeclinedLawyer_IsNotRenotified()
    {
        await _factory.ResetEmergencyStateAsync();
        var (customer, _) = await _factory.CreateAuthenticatedClientAsync(UniquePhone());
        var city = await CreateLawyerAsync(UniquePhone(), UniqueBar("DC1"), 12.9716, 77.5946);
        var state = await CreateLawyerAsync(UniquePhone(), UniqueBar("DC2"), 12.2958, 76.6394);
        var sos = await CreateSosAsync(customer, 12.9716, 77.5946);
        var cityAssignment = (await IncomingAsync(city.Client)).First(x => x.RequestId == sos.RequestId).AssignmentId;

        var decline = await city.Client.PostAsJsonAsync($"/api/emergency/assignments/{cityAssignment}/decline", new { });
        Assert.True(decline.IsSuccessStatusCode, await decline.Content.ReadAsStringAsync());

        Assert.DoesNotContain(await IncomingAsync(city.Client), x => x.RequestId == sos.RequestId);
        Assert.Contains(await IncomingAsync(state.Client), x => x.RequestId == sos.RequestId);
    }

    [Fact]
    public async Task SpecializationFilter_StillApplies()
    {
        await _factory.ResetEmergencyStateAsync();
        var (customer, _) = await _factory.CreateAuthenticatedClientAsync(UniquePhone());
        var family = await CreateLawyerAsync(
            UniquePhone(),
            UniqueBar("FAM1"),
            12.9716,
            77.5946,
            specializationId: SeedIds.Specializations.FamilyLaw);

        var sos = await CreateSosAsync(customer, 12.9716, 77.5946);
        Assert.DoesNotContain(await IncomingAsync(family.Client), x => x.RequestId == sos.RequestId);
    }

    [Fact]
    public async Task OfflineLawyer_IsNotEligible()
    {
        await _factory.ResetEmergencyStateAsync();
        var (customer, _) = await _factory.CreateAuthenticatedClientAsync(UniquePhone());
        var offline = await CreateLawyerAsync(UniquePhone(), UniqueBar("OFF1"), 12.9716, 77.5946, online: false);
        var sos = await CreateSosAsync(customer, 12.9716, 77.5946);
        Assert.DoesNotContain(await IncomingAsync(offline.Client), x => x.RequestId == sos.RequestId);
    }

    [Fact]
    public async Task EmergencyUnavailable_IsNotEligible()
    {
        await _factory.ResetEmergencyStateAsync();
        var (customer, _) = await _factory.CreateAuthenticatedClientAsync(UniquePhone());
        var closed = await CreateLawyerAsync(UniquePhone(), UniqueBar("EM1"), 12.9716, 77.5946, emergency: false);
        var sos = await CreateSosAsync(customer, 12.9716, 77.5946);
        Assert.DoesNotContain(await IncomingAsync(closed.Client), x => x.RequestId == sos.RequestId);
    }

    [Fact]
    public async Task UnverifiedLawyer_IsNotEligible()
    {
        await _factory.ResetEmergencyStateAsync();
        var (customer, _) = await _factory.CreateAuthenticatedClientAsync(UniquePhone());
        var unverified = await CreateLawyerAsync(UniquePhone(), UniqueBar("UV1"), 12.9716, 77.5946, verified: false);
        var sos = await CreateSosAsync(customer, 12.9716, 77.5946);
        Assert.DoesNotContain(await IncomingAsync(unverified.Client), x => x.RequestId == sos.RequestId);
    }

    private static string UniquePhone() => $"+91973{Random.Shared.Next(10000000, 99999999)}";

    private static string UniqueBar(string tag) => $"BAR-GEO-{tag}-{Random.Shared.Next(1000, 9999)}";

    private async Task<SosDto> CreateSosAsync(HttpClient customer, double lat, double lng)
    {
        var create = await customer.PostAsJsonAsync(
            "/api/emergency/sos",
            new
            {
                categoryId = SeedIds.EmergencyCategories.CriminalEmergency,
                latitude = lat,
                longitude = lng,
                notes = "geo matching"
            });
        Assert.True(create.IsSuccessStatusCode, await create.Content.ReadAsStringAsync());
        var sos = await create.Content.ReadFromJsonAsync<SosDto>(LawConnectWebApplicationFactory.JsonOptions);
        Assert.NotNull(sos);
        return sos;
    }

    private static async Task<IncomingDto[]> IncomingAsync(HttpClient client) =>
        await client.GetFromJsonAsync<IncomingDto[]>("/api/emergency/incoming", LawConnectWebApplicationFactory.JsonOptions)
        ?? [];

    private async Task<(HttpClient Client, Guid LawyerId)> CreateLawyerAsync(
        string phone,
        string bar,
        double lat,
        double lon,
        Guid? specializationId = null,
        bool online = true,
        bool emergency = true,
        bool verified = true)
    {
        var spec = specializationId ?? SeedIds.Specializations.CriminalLaw;
        var (client, _) = await _factory.CreateAuthenticatedClientAsync(phone);
        var register = await client.PostAsJsonAsync(
            "/api/lawyers/register",
            new
            {
                barCouncilNumber = bar,
                bio = "Geo lawyer",
                yearsOfExperience = 8,
                consultationFee = 1000m,
                emergencyFee = 2000m,
                specializationIds = new[] { spec }
            });
        Assert.True(register.IsSuccessStatusCode, await register.Content.ReadAsStringAsync());
        var profile = await register.Content.ReadFromJsonAsync<LawyerMini>(LawConnectWebApplicationFactory.JsonOptions);
        Assert.NotNull(profile);

        var address = await client.PostAsJsonAsync(
            "/api/users/me/addresses",
            new
            {
                line1 = "Chambers",
                postalCode = "560001",
                cityId = (Guid?)null,
                latitude = lat,
                longitude = lon,
                isPrimary = true,
                label = "Office"
            });
        Assert.True(address.IsSuccessStatusCode, await address.Content.ReadAsStringAsync());

        await client.PutAsJsonAsync("/api/lawyers/me/online-status", new { isOnline = online });
        await client.PutAsJsonAsync(
            "/api/lawyers/me/emergency-availability",
            new { isAvailableForEmergency = emergency, emergencyFee = 2000m });

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var lawyer = await db.Lawyers.Include(l => l.User).ThenInclude(u => u.Addresses).FirstAsync(l => l.Id == profile.LawyerId);
        lawyer.IsVerified = verified;
        lawyer.VerificationStatus = verified ? VerificationStatus.Approved : VerificationStatus.Pending;
        lawyer.IsOnline = online;
        lawyer.IsAvailableForEmergency = emergency;
        lawyer.RatingAverage = 4.5;
        lawyer.RatingCount = 10;
        await db.SaveChangesAsync();

        var (reauthed, _) = await _factory.CreateAuthenticatedClientAsync(phone);
        return (reauthed, profile.LawyerId);
    }

    private sealed record SosDto(
        Guid RequestId,
        string StatusName,
        int AttemptNumber,
        string? CityName,
        string? StateName,
        string? CountryName,
        string SearchScope);

    private sealed record IncomingDto(Guid RequestId, Guid AssignmentId, int AttemptNumber);

    private sealed record LawyerMini(Guid LawyerId);
}
