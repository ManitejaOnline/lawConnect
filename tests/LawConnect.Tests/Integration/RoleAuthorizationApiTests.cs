using System.Net;
using System.Net.Http.Json;
using LawConnect.Domain.Entities;
using LawConnect.Infrastructure.Persistence;
using LawConnect.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LawConnect.Tests.Integration;

[Collection("Integration")]
public sealed class RoleAuthorizationApiTests
{
    private readonly LawConnectWebApplicationFactory _factory;

    public RoleAuthorizationApiTests(LawConnectWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Customer_CannotAccess_LawyerOnlyOrAdminEndpoints()
    {
        var (customer, login) = await _factory.CreateAuthenticatedClientAsync("+919730000001");
        Assert.Contains(Roles.Customer, login.Roles);
        Assert.DoesNotContain(Roles.Lawyer, login.Roles);
        Assert.DoesNotContain(Roles.Admin, login.Roles);

        var incoming = await customer.GetAsync("/api/emergency/incoming");
        Assert.Equal(HttpStatusCode.Forbidden, incoming.StatusCode);

        var accept = await customer.PostAsJsonAsync(
            $"/api/emergency/assignments/{Guid.NewGuid()}/accept",
            new { });
        Assert.Equal(HttpStatusCode.Forbidden, accept.StatusCode);

        var admin = await customer.GetAsync("/api/admin/status");
        Assert.Equal(HttpStatusCode.Forbidden, admin.StatusCode);

        var metrics = await customer.GetAsync("/api/emergency/metrics");
        Assert.Equal(HttpStatusCode.Forbidden, metrics.StatusCode);
    }

    [Fact]
    public async Task Admin_CanReadEmergencyMetrics()
    {
        var (admin, _) = await CreateAdminClientAsync("+919730000099");
        var metrics = await admin.GetAsync("/api/emergency/metrics");
        Assert.Equal(HttpStatusCode.OK, metrics.StatusCode);
    }

    [Fact]
    public async Task Lawyer_CannotAccess_CustomerOnlyOrAdminEndpoints()
    {
        var (lawyer, login) = await CreateLawyerClientAsync("+919730000002", "BAR-ROLE-L1");
        Assert.Contains(Roles.Lawyer, login.Roles);
        Assert.DoesNotContain(Roles.Customer, login.Roles);
        Assert.DoesNotContain(Roles.Admin, login.Roles);

        var sos = await lawyer.PostAsJsonAsync(
            "/api/emergency/sos",
            new
            {
                categoryId = SeedIds.EmergencyCategories.CriminalEmergency,
                latitude = 12.97,
                longitude = 77.59,
                notes = "should fail"
            });
        Assert.Equal(HttpStatusCode.Forbidden, sos.StatusCode);

        var history = await lawyer.GetAsync("/api/emergency/history");
        Assert.True(history.IsSuccessStatusCode, await history.Content.ReadAsStringAsync());

        var admin = await lawyer.GetAsync("/api/admin/status");
        Assert.Equal(HttpStatusCode.Forbidden, admin.StatusCode);

        var incoming = await lawyer.GetAsync("/api/emergency/incoming");
        Assert.True(incoming.IsSuccessStatusCode, await incoming.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Admin_CanAccessAdminStatus_ButNotCustomerOrLawyerEmergencyActions()
    {
        var (admin, login) = await CreateAdminClientAsync("+919730000003");
        Assert.Contains(Roles.Admin, login.Roles);
        Assert.DoesNotContain(Roles.Customer, login.Roles);
        Assert.DoesNotContain(Roles.Lawyer, login.Roles);

        var status = await admin.GetAsync("/api/admin/status");
        Assert.True(status.IsSuccessStatusCode, await status.Content.ReadAsStringAsync());

        var sos = await admin.PostAsJsonAsync(
            "/api/emergency/sos",
            new
            {
                categoryId = SeedIds.EmergencyCategories.CriminalEmergency,
                latitude = 12.97,
                longitude = 77.59
            });
        Assert.Equal(HttpStatusCode.Forbidden, sos.StatusCode);

        var incoming = await admin.GetAsync("/api/emergency/incoming");
        Assert.Equal(HttpStatusCode.Forbidden, incoming.StatusCode);
    }

    private async Task<(HttpClient Client, LoginDto Login)> CreateLawyerClientAsync(string phone, string bar)
    {
        var (client, _) = await _factory.CreateAuthenticatedClientAsync(phone);
        var register = await client.PostAsJsonAsync(
            "/api/lawyers/register",
            new
            {
                barCouncilNumber = bar,
                bio = "Role isolation lawyer",
                yearsOfExperience = 6,
                consultationFee = 1200m,
                emergencyFee = 2000m,
                specializationIds = new[] { SeedIds.Specializations.CriminalLaw }
            });
        Assert.True(register.IsSuccessStatusCode, await register.Content.ReadAsStringAsync());

        return await _factory.CreateAuthenticatedClientAsync(phone);
    }

    private async Task<(HttpClient Client, LoginDto Login)> CreateAdminClientAsync(string phone)
    {
        var (_, firstLogin) = await _factory.CreateAuthenticatedClientAsync(phone);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var links = await db.UserRoles.Where(ur => ur.UserId == firstLogin.UserId).ToListAsync();
            db.UserRoles.RemoveRange(links);
            db.UserRoles.Add(new UserRole
            {
                UserId = firstLogin.UserId,
                RoleId = SeedIds.Roles.Admin
            });
            await db.SaveChangesAsync();
        }

        return await _factory.CreateAuthenticatedClientAsync(phone);
    }
}
