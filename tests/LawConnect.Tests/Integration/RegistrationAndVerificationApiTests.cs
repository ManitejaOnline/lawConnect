using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using LawConnect.Domain.Entities;
using LawConnect.Domain.Enums;
using LawConnect.Infrastructure.Persistence;
using LawConnect.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LawConnect.Tests.Integration;

[Collection("Integration")]
public sealed class RegistrationAndVerificationApiTests
{
    private readonly LawConnectWebApplicationFactory _factory;

    public RegistrationAndVerificationApiTests(LawConnectWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CustomerSignup_CreatesCustomerAccountAndCanLogin()
    {
        var client = _factory.CreateClient();
        var phone = UniquePhone("91");
        var email = $"user{Guid.NewGuid():N}@example.com";

        var register = await client.PostAsJsonAsync(
            "/api/auth/register/customer",
            new
            {
                fullName = "Priya Sharma",
                email,
                phoneNumber = phone,
                password = "Password1!",
                deviceIdentifier = "test-device-1",
                platform = DevicePlatform.Android
            });
        Assert.True(register.IsSuccessStatusCode, await register.Content.ReadAsStringAsync());
        var login = await register.Content.ReadFromJsonAsync<LoginDto>(LawConnectWebApplicationFactory.JsonOptions);
        Assert.NotNull(login);
        Assert.Contains(Roles.Customer, login.Roles);
        Assert.DoesNotContain(Roles.Lawyer, login.Roles);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var user = await db.Users.Include(u => u.Customer).FirstAsync(u => u.Id == login.UserId);
            Assert.NotNull(user.Customer);
            Assert.False(string.IsNullOrWhiteSpace(user.PasswordHash));
        }

        var relogin = await client.PostAsJsonAsync(
            "/api/auth/login",
            new
            {
                identifier = email,
                password = "Password1!",
                deviceIdentifier = "test-device-1",
                platform = DevicePlatform.Android
            });
        Assert.True(relogin.IsSuccessStatusCode, await relogin.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task LawyerSignup_StaysPending_UntilAdminApproves()
    {
        var phone = UniquePhone("92");
        var email = $"lawyer{Guid.NewGuid():N}@example.com";
        var (lawyerClient, lawyerLogin, lawyerId) = await RegisterPendingLawyerAsync(phone, email, "BAR-REG-1001");

        var searchBefore = await lawyerClient.GetFromJsonAsync<PagedDto<SearchItem>>(
            $"/api/lawyers/search?pageNumber=1&pageSize=50",
            LawConnectWebApplicationFactory.JsonOptions);
        Assert.DoesNotContain(searchBefore!.Items, x => x.LawyerId == lawyerId);

        var online = await lawyerClient.PutAsJsonAsync("/api/lawyers/me/online-status", new { isOnline = true });
        Assert.Equal(HttpStatusCode.Forbidden, online.StatusCode);

        var emergency = await lawyerClient.PutAsJsonAsync(
            "/api/lawyers/me/emergency-availability",
            new { isAvailableForEmergency = true, emergencyFee = 2000m });
        Assert.Equal(HttpStatusCode.Forbidden, emergency.StatusCode);

        var accept = await lawyerClient.PostAsJsonAsync(
            $"/api/emergency/assignments/{Guid.NewGuid()}/accept",
            new { });
        Assert.Equal(HttpStatusCode.Forbidden, accept.StatusCode);

        var (customer, _) = await _factory.CreateAuthenticatedClientAsync(UniquePhone("93"));
        var book = await customer.PostAsJsonAsync(
            "/api/appointments",
            new
            {
                lawyerId,
                startUtc = DateTime.UtcNow.AddDays(2),
                endUtc = DateTime.UtcNow.AddDays(2).AddMinutes(30),
                timeZoneId = "Asia/Kolkata"
            });
        Assert.False(book.IsSuccessStatusCode);

        var docs = await lawyerClient.GetFromJsonAsync<DocumentDto[]>(
            "/api/lawyers/me/documents",
            LawConnectWebApplicationFactory.JsonOptions);
        Assert.NotNull(docs);
        Assert.NotEmpty(docs);
        Assert.All(docs, d => Assert.StartsWith("/api/lawyers/me/documents/", d.DownloadPath ?? string.Empty));

        var firstDoc = docs[0];
        Assert.False(string.IsNullOrWhiteSpace(firstDoc.DownloadPath));
        var customerDoc = await customer.GetAsync(firstDoc.DownloadPath);
        Assert.True(
            customerDoc.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.NotFound,
            await customerDoc.Content.ReadAsStringAsync());

        var publicFile = await customer.GetAsync($"/files/{firstDoc.DownloadPath.TrimStart('/')}");
        Assert.True(publicFile.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden);

        var admin = await CreateAdminClientAsync(UniquePhone("94"));
        var queue = await admin.GetAsync("/api/admin/lawyers/applications?status=UnderReview");
        Assert.True(queue.IsSuccessStatusCode, await queue.Content.ReadAsStringAsync());

        var detail = await admin.GetAsync($"/api/admin/lawyers/{lawyerId}/application");
        Assert.True(detail.IsSuccessStatusCode, await detail.Content.ReadAsStringAsync());

        var adminDownload = await admin.GetAsync($"/api/admin/lawyers/{lawyerId}/documents/{firstDoc.Id}/file");
        Assert.True(adminDownload.IsSuccessStatusCode, await adminDownload.Content.ReadAsStringAsync());

        var reject = await admin.PostAsJsonAsync(
            $"/api/admin/lawyers/{lawyerId}/reject",
            new { reason = "Enrollment document is unclear." });
        Assert.True(reject.IsSuccessStatusCode, await reject.Content.ReadAsStringAsync());

        var onlineAfterReject = await lawyerClient.PutAsJsonAsync("/api/lawyers/me/online-status", new { isOnline = true });
        Assert.Equal(HttpStatusCode.Forbidden, onlineAfterReject.StatusCode);

        var reopen = await lawyerClient.PostAsync("/api/lawyers/me/application/reopen", null);
        Assert.True(reopen.IsSuccessStatusCode, await reopen.Content.ReadAsStringAsync());

        await UploadRequiredDocumentsAsync(lawyerClient);
        var resubmit = await lawyerClient.PostAsync("/api/lawyers/me/application/submit", null);
        Assert.True(resubmit.IsSuccessStatusCode, await resubmit.Content.ReadAsStringAsync());

        var approve = await admin.PostAsync($"/api/admin/lawyers/{lawyerId}/approve", null);
        Assert.True(approve.IsSuccessStatusCode, await approve.Content.ReadAsStringAsync());

        var onlineAfterApprove = await lawyerClient.PutAsJsonAsync("/api/lawyers/me/online-status", new { isOnline = true });
        Assert.True(onlineAfterApprove.IsSuccessStatusCode, await onlineAfterApprove.Content.ReadAsStringAsync());

        var searchAfter = await customer.GetFromJsonAsync<PagedDto<SearchItem>>(
            "/api/lawyers/search?pageNumber=1&pageSize=50",
            LawConnectWebApplicationFactory.JsonOptions);
        Assert.Contains(searchAfter!.Items, x => x.LawyerId == lawyerId && x.IsVerified);
    }

    private async Task<(HttpClient Client, LoginDto Login, Guid LawyerId)> RegisterPendingLawyerAsync(
        string phone,
        string email,
        string bar)
    {
        var client = _factory.CreateClient();
        var register = await client.PostAsJsonAsync(
            "/api/auth/register/lawyer",
            new
            {
                fullName = "Arjun Mehta",
                email,
                phoneNumber = phone,
                password = "Password1!",
                deviceIdentifier = "test-device-1",
                platform = DevicePlatform.Android
            });
        Assert.True(register.IsSuccessStatusCode, await register.Content.ReadAsStringAsync());
        var login = await register.Content.ReadFromJsonAsync<LoginDto>(LawConnectWebApplicationFactory.JsonOptions);
        Assert.NotNull(login);
        Assert.Contains(Roles.Lawyer, login.Roles);
        Assert.DoesNotContain(Roles.Customer, login.Roles);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.Tokens.AccessToken);

        var save = await client.PutAsJsonAsync(
            "/api/lawyers/me/application",
            new
            {
                stateBarCouncil = "Karnataka State Bar Council",
                barCouncilNumber = bar,
                specializationIds = new[] { SeedIds.Specializations.CriminalLaw },
                yearsOfExperience = 7,
                languages = "English, Hindi",
                education = "NLSIU Bangalore",
                consultationFee = 2000m,
                cityName = "Bengaluru",
                stateName = "Karnataka",
                countryName = "India",
                bio = "Criminal defence.",
                availability = new[]
                {
                    new
                    {
                        dayOfWeek = DayOfWeekFlag.Monday,
                        startTime = "09:00:00",
                        endTime = "17:00:00",
                        isAvailable = true,
                        timeZoneId = "Asia/Kolkata"
                    }
                }
            });
        Assert.True(save.IsSuccessStatusCode, await save.Content.ReadAsStringAsync());

        await UploadRequiredDocumentsAsync(client);

        var submit = await client.PostAsync("/api/lawyers/me/application/submit", null);
        Assert.True(submit.IsSuccessStatusCode, await submit.Content.ReadAsStringAsync());
        var profile = await submit.Content.ReadFromJsonAsync<LawyerMini>(LawConnectWebApplicationFactory.JsonOptions);
        Assert.NotNull(profile);
        Assert.Equal("UnderReview", profile.VerificationStatus);

        return (client, login, profile.LawyerId);
    }

    private static async Task UploadRequiredDocumentsAsync(HttpClient client)
    {
        await UploadDocumentAsync(client, DocumentType.BarCouncilCertificate, "bar.pdf");
        await UploadDocumentAsync(client, DocumentType.GovernmentId, "id.pdf");
    }

    private static async Task UploadDocumentAsync(HttpClient client, DocumentType type, string fileName)
    {
        using var form = new MultipartFormDataContent();
        form.Add(new StringContent(type.ToString()), "documentType");
        var bytes = Encoding.UTF8.GetBytes("%PDF-1.4 test document");
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        form.Add(file, "file", fileName);
        var response = await client.PostAsync("/api/lawyers/me/documents", form);
        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
    }

    private async Task<HttpClient> CreateAdminClientAsync(string phone)
    {
        var (client, login) = await _factory.CreateAuthenticatedClientAsync(phone);
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var links = await db.UserRoles.Where(ur => ur.UserId == login.UserId).ToListAsync();
            db.UserRoles.RemoveRange(links);
            db.UserRoles.Add(new UserRole { UserId = login.UserId, RoleId = SeedIds.Roles.Admin });
            await db.SaveChangesAsync();
        }

        var (admin, _) = await _factory.CreateAuthenticatedClientAsync(phone);
        return admin;
    }

    private static string UniquePhone(string prefix) => $"+91{prefix}{Random.Shared.Next(10000000, 99999999)}";

    private sealed record SearchItem(Guid LawyerId, bool IsVerified, string VerificationStatus);

    private sealed record DocumentDto(Guid Id, string? DownloadPath);

    private sealed record LawyerMini(Guid LawyerId, string VerificationStatus);

    private sealed record PagedDto<T>(IReadOnlyList<T> Items);
}
