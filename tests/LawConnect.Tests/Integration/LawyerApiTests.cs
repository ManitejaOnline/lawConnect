using System.Net.Http.Json;
using LawConnect.Domain.Enums;
using LawConnect.Shared.Constants;

namespace LawConnect.Tests.Integration;

[Collection("Integration")]
public sealed class LawyerApiTests
{
    private readonly LawConnectWebApplicationFactory _factory;

    public LawyerApiTests(LawConnectWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task RegisterUpdateFeesOnlinePracticeAreasAvailability_Flow_Succeeds()
    {
        var (client, _) = await _factory.CreateAuthenticatedClientAsync("+919822233344");

        var register = await client.PostAsJsonAsync(
            "/api/lawyers/register",
            new
            {
                barCouncilNumber = "BAR-TEST-1001",
                bio = "Civil and family law practitioner.",
                yearsOfExperience = 8,
                consultationFee = 1500m,
                emergencyFee = 2500m,
                specializationIds = new[]
                {
                    SeedIds.Specializations.CivilLaw,
                    SeedIds.Specializations.FamilyLaw
                }
            });
        Assert.True(register.IsSuccessStatusCode, await register.Content.ReadAsStringAsync());
        var profile = await register.Content.ReadFromJsonAsync<LawyerProfileDto>(LawConnectWebApplicationFactory.JsonOptions);
        Assert.NotNull(profile);
        Assert.Equal(1500m, profile.ConsultationFee);
        await _factory.ApproveLawyerAsync(profile.LawyerId);

        var fees = await client.PutAsJsonAsync(
            "/api/lawyers/me/fees",
            new { consultationFee = 1800m, emergencyFee = 3000m });
        Assert.True(fees.IsSuccessStatusCode, await fees.Content.ReadAsStringAsync());

        var online = await client.PutAsJsonAsync(
            "/api/lawyers/me/online-status",
            new { isOnline = true });
        Assert.True(online.IsSuccessStatusCode, await online.Content.ReadAsStringAsync());
        var onlineProfile = await online.Content.ReadFromJsonAsync<LawyerProfileDto>(LawConnectWebApplicationFactory.JsonOptions);
        Assert.NotNull(onlineProfile);
        Assert.True(onlineProfile.IsOnline);

        var practiceAreas = await client.PutAsJsonAsync(
            "/api/lawyers/me/practice-areas",
            new
            {
                practiceAreas = new[]
                {
                    new
                    {
                        specializationId = SeedIds.Specializations.CriminalLaw,
                        yearsOfExperience = 5,
                        isPrimary = true
                    },
                    new
                    {
                        specializationId = SeedIds.Specializations.CivilLaw,
                        yearsOfExperience = 8,
                        isPrimary = false
                    }
                }
            });
        Assert.True(practiceAreas.IsSuccessStatusCode, await practiceAreas.Content.ReadAsStringAsync());

        var availability = await client.PutAsJsonAsync(
            "/api/lawyers/me/availability",
            new
            {
                slots = new[]
                {
                    new
                    {
                        dayOfWeek = DayOfWeekFlag.Monday,
                        startTime = "09:00:00",
                        endTime = "13:00:00",
                        isAvailable = true,
                        timeZoneId = "Asia/Kolkata"
                    }
                }
            });
        Assert.True(availability.IsSuccessStatusCode, await availability.Content.ReadAsStringAsync());

        var publicProfile = await client.GetFromJsonAsync<LawyerProfileDto>(
            $"/api/lawyers/{profile.LawyerId}",
            LawConnectWebApplicationFactory.JsonOptions);
        Assert.NotNull(publicProfile);
        Assert.Equal(1800m, publicProfile.ConsultationFee);
    }

    private sealed record LawyerProfileDto(
        Guid LawyerId,
        Guid UserId,
        decimal ConsultationFee,
        bool IsOnline);
}
