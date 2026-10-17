using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using LawConnect.Application.Common.Realtime;
using LawConnect.Domain.Entities;
using LawConnect.Domain.Enums;
using LawConnect.Infrastructure.Persistence;
using LawConnect.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LawConnect.Tests.Integration;

[Collection("Integration")]
public sealed class CustomerDocumentTransferApiTests
{
    private readonly LawConnectWebApplicationFactory _factory;

    public CustomerDocumentTransferApiTests(LawConnectWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task PhoneTransfer_RejectsUnauthorizedLawyer_OfflineCustomer_ExpiredToken_AndInterruptedBytes()
    {
        var (customer, customerLogin) = await _factory.CreateAuthenticatedClientAsync(UniquePhone("81"));
        var (lawyer, lawyerId) = await CreateLawyerAsync(UniquePhone("82"), "BAR-DOC-1");
        var (other, _) = await _factory.CreateAuthenticatedClientAsync(UniquePhone("83"));
        var caseId = await CreateCaseAsync(customerLogin.UserId, lawyerId);

        var strangerRegister = await other.PostAsJsonAsync("/api/customer-documents", NewDocument(caseId, 32));
        Assert.Equal(HttpStatusCode.Forbidden, strangerRegister.StatusCode);

        await SetOnlineAsync(customerLogin.UserId, true);
        var registered = await customer.PostAsJsonAsync("/api/customer-documents", NewDocument(caseId, 24));
        Assert.True(registered.IsSuccessStatusCode, await registered.Content.ReadAsStringAsync());
        var document = await registered.Content.ReadFromJsonAsync<DocumentDto>(LawConnectWebApplicationFactory.JsonOptions);
        Assert.NotNull(document);

        await SetOnlineAsync(customerLogin.UserId, false);
        var offline = await lawyer.PostAsync($"/api/customer-documents/{document.DocumentId}/requests", null);
        Assert.Equal(HttpStatusCode.Conflict, offline.StatusCode);

        await SetOnlineAsync(customerLogin.UserId, true);
        var forbidden = await other.PostAsync($"/api/customer-documents/{document.DocumentId}/requests", null);
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);

        var requested = await lawyer.PostAsync($"/api/customer-documents/{document.DocumentId}/requests", null);
        Assert.True(requested.IsSuccessStatusCode, await requested.Content.ReadAsStringAsync());
        var session = await requested.Content.ReadFromJsonAsync<SessionDto>(LawConnectWebApplicationFactory.JsonOptions);
        Assert.NotNull(session);

        var lawyerApprove = await lawyer.PostAsync($"/api/customer-documents/transfers/{session.SessionId}/approve", null);
        Assert.Equal(HttpStatusCode.Forbidden, lawyerApprove.StatusCode);

        var approved = await customer.PostAsync($"/api/customer-documents/transfers/{session.SessionId}/approve", null);
        Assert.True(approved.IsSuccessStatusCode, await approved.Content.ReadAsStringAsync());
        var approval = await approved.Content.ReadFromJsonAsync<ApprovalDto>(LawConnectWebApplicationFactory.JsonOptions);
        Assert.NotNull(approval);
        Assert.False(string.IsNullOrWhiteSpace(approval.UploadToken));

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var row = await db.DocumentTransferSessions.FirstAsync(s => s.Id == session.SessionId);
            row.ExpiresAtUtc = DateTime.UtcNow.AddMinutes(-5);
            await db.SaveChangesAsync();
        }

        var expired = await UploadAsync(customer, session.SessionId, approval.UploadToken, new byte[24]);
        Assert.Equal(HttpStatusCode.Conflict, expired.StatusCode);

        var again = await lawyer.PostAsync($"/api/customer-documents/{document.DocumentId}/requests", null);
        Assert.True(again.IsSuccessStatusCode, await again.Content.ReadAsStringAsync());
        var second = await again.Content.ReadFromJsonAsync<SessionDto>(LawConnectWebApplicationFactory.JsonOptions);
        Assert.NotNull(second);
        var secondApproval = await customer.PostAsync($"/api/customer-documents/transfers/{second.SessionId}/approve", null);
        var token = (await secondApproval.Content.ReadFromJsonAsync<ApprovalDto>(LawConnectWebApplicationFactory.JsonOptions))!.UploadToken;

        var shortUpload = UploadAsync(customer, second.SessionId, token, new byte[4]);
        var shortDownload = lawyer.GetAsync($"/api/customer-documents/transfers/{second.SessionId}/download");
        await Task.WhenAll(shortUpload, shortDownload);
        Assert.Equal(HttpStatusCode.Conflict, (await shortUpload).StatusCode);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var row = await db.DocumentTransferSessions.FirstAsync(s => s.Id == second.SessionId);
            Assert.Equal(DocumentTransferStatus.Interrupted, row.Status);
            Assert.Null(row.UploadTokenHash);
        }
    }

    [Fact]
    public async Task PhoneTransfer_RelaysBytesOnce_WhileCustomerIsOnline()
    {
        var (customer, customerLogin) = await _factory.CreateAuthenticatedClientAsync(UniquePhone("84"));
        var (lawyer, lawyerId) = await CreateLawyerAsync(UniquePhone("85"), "BAR-DOC-2");
        var caseId = await CreateCaseAsync(customerLogin.UserId, lawyerId);
        await SetOnlineAsync(customerLogin.UserId, true);

        var payload = Encoding.ASCII.GetBytes("%PDF-1.4 phone-to-phone");
        var registered = await customer.PostAsJsonAsync("/api/customer-documents", NewDocument(caseId, payload.Length));
        var document = (await registered.Content.ReadFromJsonAsync<DocumentDto>(LawConnectWebApplicationFactory.JsonOptions))!;
        var requested = await lawyer.PostAsync($"/api/customer-documents/{document.DocumentId}/requests", null);
        var session = (await requested.Content.ReadFromJsonAsync<SessionDto>(LawConnectWebApplicationFactory.JsonOptions))!;
        var approved = await customer.PostAsync($"/api/customer-documents/transfers/{session.SessionId}/approve", null);
        var approval = (await approved.Content.ReadFromJsonAsync<ApprovalDto>(LawConnectWebApplicationFactory.JsonOptions))!;

        var uploadTask = UploadAsync(customer, session.SessionId, approval.UploadToken, payload);
        var downloadTask = lawyer.GetAsync(
            $"/api/customer-documents/transfers/{session.SessionId}/download",
            HttpCompletionOption.ResponseHeadersRead);
        await Task.WhenAll(uploadTask, downloadTask);

        var download = await downloadTask;
        Assert.True(download.IsSuccessStatusCode, await download.Content.ReadAsStringAsync());
        var received = await download.Content.ReadAsByteArrayAsync();
        Assert.Equal(payload, received);

        var reuse = await UploadAsync(customer, session.SessionId, approval.UploadToken, payload);
        Assert.Equal(HttpStatusCode.Forbidden, reuse.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var row = await db.DocumentTransferSessions.FirstAsync(s => s.Id == session.SessionId);
        Assert.Equal(DocumentTransferStatus.Completed, row.Status);
        Assert.Equal(payload.Length, row.BytesTransferred);
        Assert.Null(row.UploadTokenHash);
        Assert.DoesNotContain(db.Model.GetEntityTypes(), t => t.ClrType == typeof(CustomerDocument) && t.GetProperties().Any(p => p.Name.Contains("Path", StringComparison.OrdinalIgnoreCase)));
    }

    private async Task<HttpResponseMessage> UploadAsync(HttpClient customer, Guid sessionId, string token, byte[] bytes)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/customer-documents/transfers/{sessionId}/upload")
        {
            Content = new ByteArrayContent(bytes)
        };
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        request.Headers.Add("X-Transfer-Token", token);
        return await customer.SendAsync(request);
    }

    private async Task SetOnlineAsync(Guid userId, bool online)
    {
        var presence = _factory.Services.GetRequiredService<IPresenceTracker>();
        if (online)
        {
            await presence.UserConnectedAsync(userId, "doc-test");
        }
        else
        {
            await presence.UserDisconnectedAsync(userId, "doc-test");
        }
    }

    private async Task<(HttpClient Client, Guid LawyerId)> CreateLawyerAsync(string phone, string bar)
    {
        var (client, _) = await _factory.CreateAuthenticatedClientAsync(phone);
        var register = await client.PostAsJsonAsync("/api/lawyers/register", new
        {
            barCouncilNumber = bar,
            bio = "Document lawyer",
            yearsOfExperience = 4,
            consultationFee = 500m,
            emergencyFee = 800m,
            specializationIds = new[] { SeedIds.Specializations.CivilLaw }
        });
        Assert.True(register.IsSuccessStatusCode, await register.Content.ReadAsStringAsync());
        var profile = await register.Content.ReadFromJsonAsync<LawyerDto>(LawConnectWebApplicationFactory.JsonOptions);
        Assert.NotNull(profile);
        await _factory.ApproveLawyerAsync(profile.LawyerId);
        var (reauthed, _) = await _factory.CreateAuthenticatedClientAsync(phone);
        return (reauthed, profile.LawyerId);
    }

    private async Task<Guid> CreateCaseAsync(Guid customerUserId, Guid lawyerId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var customer = await db.Customers.FirstOrDefaultAsync(c => c.UserId == customerUserId);
        if (customer is null)
        {
            customer = new Customer { UserId = customerUserId };
            db.Customers.Add(customer);
            await db.SaveChangesAsync();
        }

        var matter = new Case
        {
            CustomerId = customer.Id,
            LawyerId = lawyerId,
            SpecializationId = SeedIds.Specializations.CivilLaw,
            Title = "Phone document matter",
            Status = CaseStatus.Active,
            Priority = CasePriority.Medium,
            StartDate = DateTime.UtcNow.Date
        };
        db.Cases.Add(matter);
        await db.SaveChangesAsync();
        return matter.Id;
    }

    private static object NewDocument(Guid caseId, int size) => new
    {
        caseId,
        appointmentId = (Guid?)null,
        fileName = "brief.pdf",
        contentType = "application/pdf",
        fileSizeBytes = size
    };

    private static string UniquePhone(string prefix) => $"+91{prefix}{Random.Shared.Next(1000000, 9999999)}";

    private sealed record DocumentDto(Guid DocumentId);
    private sealed record SessionDto(Guid SessionId);
    private sealed record ApprovalDto(Guid SessionId, string UploadToken);
    private sealed record LawyerDto(Guid LawyerId);
}
