using System.Net.Http.Json;
using LawConnect.Application.Common.Interfaces;
using LawConnect.Domain.Enums;
using LawConnect.Infrastructure.Emergency;
using LawConnect.Infrastructure.Persistence;
using LawConnect.Shared.Constants;
using Microsoft.Extensions.DependencyInjection;

namespace LawConnect.Tests.Integration;

[Collection("Integration")]
public sealed class EmergencySosApiTests
{
    private readonly LawConnectWebApplicationFactory _factory;

    public EmergencySosApiTests(LawConnectWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ConcurrentAccept_FirstWinsAndCancelsOthers()
    {
        var (customer, _) = await _factory.CreateAuthenticatedClientAsync("+919710000001");
        var lawyerA = await CreateEligibleLawyerAsync("+919710000002", "BAR-SOS-A1", 12.97, 77.59, rating: 4.8, years: 12);
        var lawyerB = await CreateEligibleLawyerAsync("+919710000003", "BAR-SOS-B1", 12.975, 77.595, rating: 4.5, years: 8);

        var create = await customer.PostAsJsonAsync(
            "/api/emergency/sos",
            new
            {
                categoryId = SeedIds.EmergencyCategories.CriminalEmergency,
                latitude = 12.97,
                longitude = 77.59,
                notes = "Need immediate help"
            });
        Assert.True(create.IsSuccessStatusCode, await create.Content.ReadAsStringAsync());
        var sos = await create.Content.ReadFromJsonAsync<EmergencySosDto>(LawConnectWebApplicationFactory.JsonOptions);
        Assert.NotNull(sos);
        Assert.Equal(nameof(EmergencyStatus.Notifying), sos.StatusName);

        var incomingA = await lawyerA.Client.GetFromJsonAsync<IncomingSosDto[]>(
            "/api/emergency/incoming",
            LawConnectWebApplicationFactory.JsonOptions);
        var incomingB = await lawyerB.Client.GetFromJsonAsync<IncomingSosDto[]>(
            "/api/emergency/incoming",
            LawConnectWebApplicationFactory.JsonOptions);
        Assert.NotNull(incomingA);
        Assert.NotNull(incomingB);
        Assert.NotEmpty(incomingA);
        Assert.NotEmpty(incomingB);

        var assignmentA = incomingA.First(x => x.RequestId == sos.RequestId).AssignmentId;
        var assignmentB = incomingB.First(x => x.RequestId == sos.RequestId).AssignmentId;

        var acceptTasks = new[]
        {
            lawyerA.Client.PostAsJsonAsync($"/api/emergency/assignments/{assignmentA}/accept", new { }),
            lawyerB.Client.PostAsJsonAsync($"/api/emergency/assignments/{assignmentB}/accept", new { })
        };

        var results = await Task.WhenAll(acceptTasks);
        var successCount = results.Count(r => r.IsSuccessStatusCode);
        var conflictCount = results.Count(r => r.StatusCode == System.Net.HttpStatusCode.Conflict);
        Assert.Equal(1, successCount);
        Assert.Equal(1, conflictCount);

        var winner = results.First(r => r.IsSuccessStatusCode);
        var assigned = await winner.Content.ReadFromJsonAsync<EmergencySosDto>(
            LawConnectWebApplicationFactory.JsonOptions);
        Assert.NotNull(assigned);
        Assert.Equal(nameof(EmergencyStatus.Assigned), assigned.StatusName);
        Assert.NotNull(assigned.AssignedLawyerId);

        var timeline = await customer.GetFromJsonAsync<TimelineDto[]>(
            $"/api/emergency/sos/{sos.RequestId}/timeline",
            LawConnectWebApplicationFactory.JsonOptions);
        Assert.NotNull(timeline);
        Assert.Contains(timeline, t => t.EventType == EmergencyTimelineEventTypes.LawyerAccepted);
        Assert.Contains(timeline, t => t.EventType == EmergencyTimelineEventTypes.AssignmentsCancelled);
    }

    [Fact]
    public async Task EmptyFirstRadius_ExpandsAndKeepsSearching()
    {
        await _factory.ResetEmergencyStateAsync();
        var (customer, _) = await _factory.CreateAuthenticatedClientAsync("+919710000011");
        // Isolated coords so shared InMemory lawyers from other tests cannot match.
        // ~25.5 km away: skip 10 km, likely skip 25 km, notify at 50 km.
        var lawyer = await CreateEligibleLawyerAsync("+919710000012", "BAR-SOS-T1", 12.2958, 76.6394, rating: 4.2, years: 6);

        var create = await customer.PostAsJsonAsync(
            "/api/emergency/sos",
            new
            {
                categoryId = SeedIds.EmergencyCategories.CriminalEmergency,
                latitude = 12.9716,
                longitude = 77.5946,
                notes = "State expand test"
            });
        Assert.True(create.IsSuccessStatusCode, await create.Content.ReadAsStringAsync());
        var sos = await create.Content.ReadFromJsonAsync<EmergencySosDto>(LawConnectWebApplicationFactory.JsonOptions);
        Assert.NotNull(sos);
        Assert.True(
            sos.StatusName is nameof(EmergencyStatus.Notifying) or nameof(EmergencyStatus.Searching),
            sos.StatusName);
        Assert.NotEqual(nameof(EmergencyStatus.Failed), sos.StatusName);
        Assert.NotEqual(nameof(EmergencyStatus.Expired), sos.StatusName);
        Assert.True(sos.AttemptNumber >= 2, $"Expected city→state escalation, attempt={sos.AttemptNumber}");

        var incoming = await lawyer.Client.GetFromJsonAsync<IncomingSosDto[]>(
            "/api/emergency/incoming",
            LawConnectWebApplicationFactory.JsonOptions);
        Assert.NotNull(incoming);
        Assert.Contains(incoming, x => x.RequestId == sos.RequestId);

        var active = await customer.GetAsync("/api/emergency/sos/active");
        Assert.Equal(System.Net.HttpStatusCode.OK, active.StatusCode);
    }

    [Fact]
    public async Task Timeout_DoesNotGloballyExpireSos()
    {
        var (customer, _) = await _factory.CreateAuthenticatedClientAsync("+919710000071");
        var lawyer = await CreateEligibleLawyerAsync("+919710000072", "BAR-SOS-T2", 12.9716, 77.5946, rating: 4.1, years: 5);

        var create = await customer.PostAsJsonAsync(
            "/api/emergency/sos",
            new
            {
                categoryId = SeedIds.EmergencyCategories.CriminalEmergency,
                latitude = 12.9716,
                longitude = 77.5946,
                notes = "Timeout keep-alive"
            });
        Assert.True(create.IsSuccessStatusCode, await create.Content.ReadAsStringAsync());
        var sos = await create.Content.ReadFromJsonAsync<EmergencySosDto>(LawConnectWebApplicationFactory.JsonOptions);
        Assert.NotNull(sos);
        Assert.Equal(nameof(EmergencyStatus.Notifying), sos.StatusName);

        var incomingBefore = await lawyer.Client.GetFromJsonAsync<IncomingSosDto[]>(
            "/api/emergency/incoming",
            LawConnectWebApplicationFactory.JsonOptions);
        var assignmentId = incomingBefore!.First(x => x.RequestId == sos.RequestId).AssignmentId;

        using var scope = _factory.Services.CreateScope();
        var processor = scope.ServiceProvider.GetRequiredService<IEmergencyTimeoutProcessor>();
        await processor.ProcessTimeoutAsync(sos.RequestId, sos.AttemptNumber);

        var after = await customer.GetFromJsonAsync<EmergencySosDto>(
            $"/api/emergency/sos/{sos.RequestId}",
            LawConnectWebApplicationFactory.JsonOptions);
        Assert.NotNull(after);
        Assert.NotEqual(nameof(EmergencyStatus.Expired), after.StatusName);
        Assert.NotEqual(nameof(EmergencyStatus.Failed), after.StatusName);
        Assert.True(after.AttemptNumber > sos.AttemptNumber);
        Assert.True(
            after.StatusName is nameof(EmergencyStatus.Searching) or nameof(EmergencyStatus.Notifying),
            after.StatusName);

        var incomingAfter = await lawyer.Client.GetFromJsonAsync<IncomingSosDto[]>(
            "/api/emergency/incoming",
            LawConnectWebApplicationFactory.JsonOptions);
        Assert.Contains(incomingAfter!, x => x.AssignmentId == assignmentId);

        var accept = await lawyer.Client.PostAsJsonAsync(
            $"/api/emergency/assignments/{assignmentId}/accept",
            new { });
        Assert.True(accept.IsSuccessStatusCode, await accept.Content.ReadAsStringAsync());
        var assigned = await accept.Content.ReadFromJsonAsync<EmergencySosDto>(
            LawConnectWebApplicationFactory.JsonOptions);
        Assert.Equal(nameof(EmergencyStatus.Assigned), assigned!.StatusName);
    }

    [Fact]
    public async Task CreateSos_ReturnsSearchingOrNotifying_NotFailed()
    {
        await _factory.ResetEmergencyStateAsync();
        var (customer, _) = await _factory.CreateAuthenticatedClientAsync("+919710000081");
        var create = await customer.PostAsJsonAsync(
            "/api/emergency/sos",
            new
            {
                categoryId = SeedIds.EmergencyCategories.CriminalEmergency,
                latitude = 1.23,
                longitude = 2.34,
                notes = "Empty radius still active"
            });
        Assert.True(create.IsSuccessStatusCode, await create.Content.ReadAsStringAsync());
        var sos = await create.Content.ReadFromJsonAsync<EmergencySosDto>(LawConnectWebApplicationFactory.JsonOptions);
        Assert.NotNull(sos);
        Assert.True(
            sos.StatusName is nameof(EmergencyStatus.Created)
                or nameof(EmergencyStatus.Searching)
                or nameof(EmergencyStatus.Notifying),
            sos.StatusName);
        Assert.NotEqual(nameof(EmergencyStatus.Failed), sos.StatusName);
        Assert.NotEqual(nameof(EmergencyStatus.Expired), sos.StatusName);
    }

    [Fact]
    public async Task DeclineAll_EscalatesImmediately()
    {
        var (customer, _) = await _factory.CreateAuthenticatedClientAsync("+919710000041");
        var lawyer = await CreateEligibleLawyerAsync("+919710000042", "BAR-SOS-DEC1", 19.0761, 72.8778, rating: 4.4, years: 7);

        var create = await customer.PostAsJsonAsync(
            "/api/emergency/sos",
            new
            {
                categoryId = SeedIds.EmergencyCategories.CriminalEmergency,
                latitude = 19.0760,
                longitude = 72.8777
            });
        var sos = await create.Content.ReadFromJsonAsync<EmergencySosDto>(LawConnectWebApplicationFactory.JsonOptions);
        Assert.NotNull(sos);

        var incoming = await lawyer.Client.GetFromJsonAsync<IncomingSosDto[]>(
            "/api/emergency/incoming",
            LawConnectWebApplicationFactory.JsonOptions);
        var assignmentId = incoming!.First(x => x.RequestId == sos.RequestId).AssignmentId;
        var decline = await lawyer.Client.PostAsJsonAsync($"/api/emergency/assignments/{assignmentId}/decline", new { });
        Assert.True(decline.IsSuccessStatusCode, await decline.Content.ReadAsStringAsync());

        var after = await customer.GetFromJsonAsync<EmergencySosDto>(
            $"/api/emergency/sos/{sos.RequestId}",
            LawConnectWebApplicationFactory.JsonOptions);
        Assert.NotNull(after);
        Assert.True(after.AttemptNumber > sos.AttemptNumber);
        Assert.NotEqual(nameof(EmergencyStatus.Failed), after.StatusName);
    }

    [Fact]
    public async Task CancelThenAccept_Fails()
    {
        var (customer, _) = await _factory.CreateAuthenticatedClientAsync("+919710000051");
        var lawyer = await CreateEligibleLawyerAsync("+919710000052", "BAR-SOS-C1", 26.9124, 75.7873, rating: 4.6, years: 9);

        var create = await customer.PostAsJsonAsync(
            "/api/emergency/sos",
            new
            {
                categoryId = SeedIds.EmergencyCategories.CriminalEmergency,
                latitude = 26.9125,
                longitude = 75.7874
            });
        var sos = await create.Content.ReadFromJsonAsync<EmergencySosDto>(LawConnectWebApplicationFactory.JsonOptions);
        Assert.NotNull(sos);

        var incoming = await lawyer.Client.GetFromJsonAsync<IncomingSosDto[]>(
            "/api/emergency/incoming",
            LawConnectWebApplicationFactory.JsonOptions);
        var assignmentId = incoming!.First(x => x.RequestId == sos.RequestId).AssignmentId;

        var cancel = await customer.PostAsJsonAsync($"/api/emergency/sos/{sos.RequestId}/cancel", new { reason = "Changed mind" });
        Assert.True(cancel.IsSuccessStatusCode, await cancel.Content.ReadAsStringAsync());

        var accept = await lawyer.Client.PostAsJsonAsync($"/api/emergency/assignments/{assignmentId}/accept", new { });
        Assert.Equal(System.Net.HttpStatusCode.Conflict, accept.StatusCode);
    }

    [Fact]
    public async Task Ranking_PrefersHigherRatedLawyerInSameCity()
    {
        var (customer, _) = await _factory.CreateAuthenticatedClientAsync("+919710000021");
        var near = await CreateEligibleLawyerAsync("+919710000022", "BAR-SOS-R1", 12.971, 77.591, rating: 4.9, years: 15);
        var far = await CreateEligibleLawyerAsync("+919710000023", "BAR-SOS-R2", 12.99, 77.62, rating: 3.2, years: 3);

        var create = await customer.PostAsJsonAsync(
            "/api/emergency/sos",
            new
            {
                categoryId = SeedIds.EmergencyCategories.CriminalEmergency,
                latitude = 12.97,
                longitude = 77.59,
                notes = "Ranking test"
            });
        Assert.True(create.IsSuccessStatusCode, await create.Content.ReadAsStringAsync());

        var incomingNear = await near.Client.GetFromJsonAsync<IncomingSosDto[]>(
            "/api/emergency/incoming",
            LawConnectWebApplicationFactory.JsonOptions);
        var incomingFar = await far.Client.GetFromJsonAsync<IncomingSosDto[]>(
            "/api/emergency/incoming",
            LawConnectWebApplicationFactory.JsonOptions);
        Assert.NotNull(incomingNear);
        Assert.NotNull(incomingFar);
        Assert.NotEmpty(incomingNear);
        Assert.NotEmpty(incomingFar);

        Assert.True(incomingNear[0].Rank < incomingFar[0].Rank);
    }

    [Fact]
    public async Task DuplicateConcurrentSos_IsRejected()
    {
        var (customer, _) = await _factory.CreateAuthenticatedClientAsync("+919710000031");
        await CreateEligibleLawyerAsync("+919710000032", "BAR-SOS-D1", 12.97, 77.59, rating: 4.0, years: 5);

        var first = await customer.PostAsJsonAsync(
            "/api/emergency/sos",
            new
            {
                categoryId = SeedIds.EmergencyCategories.CriminalEmergency,
                latitude = 12.97,
                longitude = 77.59,
                notes = "first"
            });
        Assert.True(first.IsSuccessStatusCode, await first.Content.ReadAsStringAsync());

        var second = await customer.PostAsJsonAsync(
            "/api/emergency/sos",
            new
            {
                categoryId = SeedIds.EmergencyCategories.CriminalEmergency,
                latitude = 12.97,
                longitude = 77.59,
                notes = "second"
            });
        Assert.Equal(System.Net.HttpStatusCode.Conflict, second.StatusCode);
    }

    private async Task<(HttpClient Client, Guid LawyerId)> CreateEligibleLawyerAsync(
        string phone,
        string bar,
        double lat,
        double lon,
        double rating,
        int years)
    {
        var (client, _) = await _factory.CreateAuthenticatedClientAsync(phone);
        var register = await client.PostAsJsonAsync(
            "/api/lawyers/register",
            new
            {
                barCouncilNumber = bar,
                bio = "Emergency lawyer",
                yearsOfExperience = years,
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
                line1 = "SOS Address",
                line2 = (string?)null,
                postalCode = "560001",
                landmark = (string?)null,
                cityId = (Guid?)null,
                latitude = lat,
                longitude = lon,
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
        lawyer.RatingAverage = rating;
        lawyer.RatingCount = 20;
        await db.SaveChangesAsync();

        // Re-authenticate so JWT includes Lawyer role.
        var (reauthed, _) = await _factory.CreateAuthenticatedClientAsync(phone);
        return (reauthed, profile.LawyerId);
    }

    private sealed record EmergencySosDto(
        Guid RequestId,
        Guid CustomerId,
        Guid CustomerUserId,
        Guid CategoryId,
        string CategoryName,
        string StatusName,
        int AttemptNumber,
        double CurrentRadiusKm,
        Guid? AssignedLawyerId,
        Guid? AcceptedAssignmentId);

    private sealed record IncomingSosDto(
        Guid RequestId,
        Guid AssignmentId,
        double DistanceKm,
        int Rank,
        int AttemptNumber);

    private sealed record TimelineDto(Guid Id, string EventType, string Message);

    private sealed record LawyerProfileMiniDto(Guid LawyerId);
}
