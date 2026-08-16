using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using LawConnect.Application.Features.Cases.Services;
using LawConnect.Application.Features.Lawyers.Services;
using LawConnect.Domain.Entities;
using LawConnect.Domain.Enums;
using LawConnect.Infrastructure.Persistence;
using LawConnect.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LawConnect.Tests.Integration;

[Collection("Integration")]
public sealed class CaseManagementApiTests
{
    private readonly LawConnectWebApplicationFactory _factory;

    public CaseManagementApiTests(LawConnectWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateCase_ForLawyerWithEstablishedRelationship_Succeeds()
    {
        var (lawyer, lawyerId, customer, _) = await SetUpRelationshipAsync(UniquePhone("90"), UniquePhone("91"), "BAR-CM-0001");

        var create = await lawyer.PostAsJsonAsync("/api/cases", NewCaseRequest(customer.CustomerId));
        Assert.True(create.IsSuccessStatusCode, await create.Content.ReadAsStringAsync());

        var body = await create.Content.ReadFromJsonAsync<CaseDto>(LawConnectWebApplicationFactory.JsonOptions);
        Assert.NotNull(body);
        Assert.Equal("Active", body!.Status);
        Assert.Equal(lawyerId, body.LawyerId);
    }

    [Fact]
    public async Task CreateCase_WithoutAppointmentRelationship_Rejected()
    {
        var (lawyer, _, _, _) = await SetUpRelationshipAsync(UniquePhone("92"), UniquePhone("93"), "BAR-CM-0002");
        var (uninvolvedCustomer, _) = await _factory.CreateAuthenticatedClientAsync(UniquePhone("94"));
        var uninvolvedCustomerId = await GetCustomerIdAsync(uninvolvedCustomer);

        var create = await lawyer.PostAsJsonAsync("/api/cases", NewCaseRequest(uninvolvedCustomerId));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, create.StatusCode);
    }

    [Fact]
    public async Task CaseLifecycle_UpdatesHearingsMilestones_ProduceOrderedTimelineAndUpcomingAction()
    {
        var (lawyer, lawyerId, customer, _) = await SetUpRelationshipAsync(UniquePhone("95"), UniquePhone("96"), "BAR-CM-0003");
        var caseId = await CreateCaseAsync(lawyer, customer.CustomerId);

        var update = await lawyer.PostAsJsonAsync($"/api/cases/{caseId}/updates",
            new { updateType = CaseUpdateType.ClientCommunication, title = "Called client", description = "Discussed next steps." });
        Assert.True(update.IsSuccessStatusCode, await update.Content.ReadAsStringAsync());

        var futureHearing = DateTime.UtcNow.AddDays(10);
        var hearing = await lawyer.PostAsJsonAsync($"/api/cases/{caseId}/hearings",
            new { hearingDate = futureHearing, courtName = "District Court", hearingType = "First Hearing", purpose = "Filing" });
        Assert.True(hearing.IsSuccessStatusCode, await hearing.Content.ReadAsStringAsync());
        var hearingBody = await hearing.Content.ReadFromJsonAsync<HearingDto>(LawConnectWebApplicationFactory.JsonOptions);
        Assert.NotNull(hearingBody);

        var milestone = await lawyer.PostAsJsonAsync($"/api/cases/{caseId}/milestones",
            new { title = "Case Filed", description = (string?)null, dueDate = DateTime.UtcNow.AddDays(3) });
        Assert.True(milestone.IsSuccessStatusCode, await milestone.Content.ReadAsStringAsync());
        var milestoneBody = await milestone.Content.ReadFromJsonAsync<MilestoneDto>(LawConnectWebApplicationFactory.JsonOptions);
        Assert.NotNull(milestoneBody);

        // Upcoming action must be the sooner of the two: the milestone (day 3) beats the hearing (day 10).
        var detail = await customer.Client.GetFromJsonAsync<CaseDetailDto>($"/api/cases/{caseId}", LawConnectWebApplicationFactory.JsonOptions);
        Assert.NotNull(detail);
        Assert.NotNull(detail!.UpcomingAction);
        Assert.Equal("Milestone", detail.UpcomingAction!.Kind);

        var outcome = await lawyer.PostAsJsonAsync($"/api/cases/{caseId}/hearings/{hearingBody!.Id}/outcome",
            new { status = HearingStatus.Adjourned, outcome = "Adjourned for lack of time", notes = "Reschedule requested", nextAction = "Await new date" });
        Assert.True(outcome.IsSuccessStatusCode, await outcome.Content.ReadAsStringAsync());

        var complete = await lawyer.PostAsync($"/api/cases/{caseId}/milestones/{milestoneBody!.Id}/complete", null);
        Assert.True(complete.IsSuccessStatusCode, await complete.Content.ReadAsStringAsync());

        var status = await lawyer.PostAsJsonAsync($"/api/cases/{caseId}/status", new { status = CaseStatus.OnHold, note = "Awaiting new hearing date" });
        Assert.True(status.IsSuccessStatusCode, await status.Content.ReadAsStringAsync());

        var timeline = await customer.Client.GetFromJsonAsync<List<TimelineEntryDto>>($"/api/cases/{caseId}/timeline", LawConnectWebApplicationFactory.JsonOptions);
        Assert.NotNull(timeline);
        Assert.True(timeline!.Count >= 4, $"expected case-created + update + hearing + milestone + status-change entries, got {timeline.Count}");
        for (var i = 1; i < timeline.Count; i++)
        {
            Assert.True(timeline[i - 1].OccurredAtUtc >= timeline[i].OccurredAtUtc, "timeline must be newest first");
        }
        Assert.Contains(timeline, e => e.Kind == "Update" && e.Status == "StatusChange");
    }

    [Fact]
    public async Task Customer_CannotCreateCaseUpdatesHearingsOrMilestones_Returns403()
    {
        var (lawyer, _, customer, _) = await SetUpRelationshipAsync(UniquePhone("97"), UniquePhone("98"), "BAR-CM-0004");
        var caseId = await CreateCaseAsync(lawyer, customer.CustomerId);

        var update = await customer.Client.PostAsJsonAsync($"/api/cases/{caseId}/updates",
            new { updateType = CaseUpdateType.General, title = "x", description = "y" });
        Assert.Equal(HttpStatusCode.Forbidden, update.StatusCode);

        var hearing = await customer.Client.PostAsJsonAsync($"/api/cases/{caseId}/hearings",
            new { hearingDate = DateTime.UtcNow.AddDays(5), courtName = (string?)null, hearingType = (string?)null, purpose = (string?)null });
        Assert.Equal(HttpStatusCode.Forbidden, hearing.StatusCode);

        var milestone = await customer.Client.PostAsJsonAsync($"/api/cases/{caseId}/milestones",
            new { title = "x", description = (string?)null, dueDate = (DateTime?)null });
        Assert.Equal(HttpStatusCode.Forbidden, milestone.StatusCode);

        var status = await customer.Client.PostAsJsonAsync($"/api/cases/{caseId}/status", new { status = CaseStatus.Closed, note = (string?)null });
        Assert.Equal(HttpStatusCode.Forbidden, status.StatusCode);
    }

    [Fact]
    public async Task Customer_CanReadOwnCase_ButNotAnotherCustomersCase()
    {
        var (lawyer, _, customer, _) = await SetUpRelationshipAsync(UniquePhone("99"), UniquePhone("00"), "BAR-CM-0005");
        var caseId = await CreateCaseAsync(lawyer, customer.CustomerId);

        var own = await customer.Client.GetAsync($"/api/cases/{caseId}");
        Assert.True(own.IsSuccessStatusCode, await own.Content.ReadAsStringAsync());

        var (otherCustomer, _) = await _factory.CreateAuthenticatedClientAsync(UniquePhone("01"));
        var other = await otherCustomer.GetAsync($"/api/cases/{caseId}");
        Assert.Equal(HttpStatusCode.Forbidden, other.StatusCode);
    }

    [Fact]
    public async Task Lawyer_CannotAccessAnotherLawyersCase()
    {
        var (lawyerA, _, customerA, _) = await SetUpRelationshipAsync(UniquePhone("02"), UniquePhone("03"), "BAR-CM-0006");
        var caseId = await CreateCaseAsync(lawyerA, customerA.CustomerId);

        var (lawyerB, _, _, _) = await SetUpRelationshipAsync(UniquePhone("04"), UniquePhone("05"), "BAR-CM-0007");
        var response = await lawyerB.GetAsync($"/api/cases/{caseId}");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task OverdueSweep_FlagsPastDueMilestones_AndAdminCanFilterForThem()
    {
        var (lawyer, lawyerId, customer, admin) = await SetUpRelationshipAsync(UniquePhone("06"), UniquePhone("07"), "BAR-CM-0008");
        var caseId = await CreateCaseAsync(lawyer, customer.CustomerId);

        var milestone = await lawyer.PostAsJsonAsync($"/api/cases/{caseId}/milestones",
            new { title = "Response due", description = (string?)null, dueDate = DateTime.UtcNow.AddDays(2) });
        Assert.True(milestone.IsSuccessStatusCode, await milestone.Content.ReadAsStringAsync());
        var milestoneBody = await milestone.Content.ReadFromJsonAsync<MilestoneDto>(LawConnectWebApplicationFactory.JsonOptions);
        Assert.NotNull(milestoneBody);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var entity = await db.CaseMilestones.FirstAsync(m => m.Id == milestoneBody!.Id);
            entity.DueDate = DateTime.UtcNow.AddDays(-1);
            await db.SaveChangesAsync();
        }

        using (var scope = _factory.Services.CreateScope())
        {
            var sweep = scope.ServiceProvider.GetRequiredService<IOverdueMilestoneSweepService>();
            await sweep.SweepAsync();
        }

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var entity = await db.CaseMilestones.FirstAsync(m => m.Id == milestoneBody!.Id);
            Assert.Equal(MilestoneStatus.Overdue, entity.Status);
        }

        var overdueList = await admin.GetFromJsonAsync<PagedDto<AdminCaseListItemDto>>(
            $"/api/admin/cases?overdueOnly=true", LawConnectWebApplicationFactory.JsonOptions);
        Assert.NotNull(overdueList);
        Assert.Contains(overdueList!.Items, c => c.Id == caseId);

        var activity = await admin.GetFromJsonAsync<CaseActivitySummaryDto>(
            $"/api/admin/lawyers/{lawyerId}/case-activity", LawConnectWebApplicationFactory.JsonOptions);
        Assert.NotNull(activity);
        Assert.True(activity!.OverdueMilestoneCount >= 1);
    }

    [Fact]
    public async Task Admin_CanViewAnyCase_WithFullDetail()
    {
        var (lawyer, _, customer, admin) = await SetUpRelationshipAsync(UniquePhone("08"), UniquePhone("09"), "BAR-CM-0009");
        var caseId = await CreateCaseAsync(lawyer, customer.CustomerId);

        var response = await admin.GetAsync($"/api/admin/cases/{caseId}");
        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());

        var detail = await response.Content.ReadFromJsonAsync<AdminCaseDetailDto>(LawConnectWebApplicationFactory.JsonOptions);
        Assert.NotNull(detail);
        Assert.Equal(caseId, detail!.Case.Id);
    }

    [Fact]
    public async Task DocumentUpload_CustomerUpload_DoesNotResetLawyerTimer_LawyerUpload_Does()
    {
        var (lawyer, lawyerId, customer, _) = await SetUpRelationshipAsync(UniquePhone("10"), UniquePhone("11"), "BAR-CM-0010");
        var caseId = await CreateCaseAsync(lawyer, customer.CustomerId);

        // Push the case's creation date (the baseline for "last meaningful lawyer activity") into the
        // past so a customer-only upload has something to (incorrectly) reset, if it were buggy.
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var entity = await db.Cases.FirstAsync(c => c.Id == caseId);
            entity.CreatedAt = DateTime.UtcNow.AddDays(-20);
            await db.SaveChangesAsync();
        }

        var customerUpload = await UploadCaseDocumentAsync(customer.Client, caseId, "evidence.pdf", "application/pdf");
        Assert.True(customerUpload.IsSuccessStatusCode, await customerUpload.Content.ReadAsStringAsync());

        var afterCustomerUpload = await customer.Client.GetFromJsonAsync<CaseDetailDto>($"/api/cases/{caseId}", LawConnectWebApplicationFactory.JsonOptions);
        Assert.NotNull(afterCustomerUpload);
        Assert.True(afterCustomerUpload!.ActivitySignals!.DaysSinceLastMeaningfulLawyerActivity >= 19,
            "customer upload must not reset the lawyer's meaningful-activity timer");

        var timelineAfterCustomer = await customer.Client.GetFromJsonAsync<List<TimelineEntryDto>>($"/api/cases/{caseId}/timeline", LawConnectWebApplicationFactory.JsonOptions);
        Assert.Contains(timelineAfterCustomer!, e => e.Kind == "Update" && e.Status == "DocumentReceived");

        var lawyerUpload = await UploadCaseDocumentAsync(lawyer, caseId, "contract.docx",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document");
        Assert.True(lawyerUpload.IsSuccessStatusCode, await lawyerUpload.Content.ReadAsStringAsync());

        var afterLawyerUpload = await customer.Client.GetFromJsonAsync<CaseDetailDto>($"/api/cases/{caseId}", LawConnectWebApplicationFactory.JsonOptions);
        Assert.NotNull(afterLawyerUpload);
        Assert.True(afterLawyerUpload!.ActivitySignals!.DaysSinceLastMeaningfulLawyerActivity <= 1,
            "a lawyer upload must reset the lawyer's meaningful-activity timer");

        var timelineAfterLawyer = await customer.Client.GetFromJsonAsync<List<TimelineEntryDto>>($"/api/cases/{caseId}/timeline", LawConnectWebApplicationFactory.JsonOptions);
        Assert.Contains(timelineAfterLawyer!, e => e.Kind == "Update" && e.Status == "DocumentFiled");
    }

    [Fact]
    public async Task DocumentUpload_CustomerWithoutAccessToCase_Returns403()
    {
        var (lawyer, _, customer, _) = await SetUpRelationshipAsync(UniquePhone("12"), UniquePhone("13"), "BAR-CM-0011");
        var caseId = await CreateCaseAsync(lawyer, customer.CustomerId);

        var (uninvolvedCustomer, _) = await _factory.CreateAuthenticatedClientAsync(UniquePhone("14"));
        var response = await UploadCaseDocumentAsync(uninvolvedCustomer, caseId, "x.pdf", "application/pdf");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DeleteDocument_PermissionMatrix_UploaderAndCaseLawyerCanDelete_OthersCannot()
    {
        var (lawyer, _, customer, _) = await SetUpRelationshipAsync(UniquePhone("15"), UniquePhone("16"), "BAR-CM-0012");
        var caseId = await CreateCaseAsync(lawyer, customer.CustomerId);

        var customerUpload = await UploadCaseDocumentAsync(customer.Client, caseId, "own.pdf", "application/pdf");
        var customerDoc = await customerUpload.Content.ReadFromJsonAsync<CaseDocumentDto>(LawConnectWebApplicationFactory.JsonOptions);
        Assert.NotNull(customerDoc);

        var (otherCustomer, _) = await _factory.CreateAuthenticatedClientAsync(UniquePhone("17"));
        var forbiddenDelete = await otherCustomer.DeleteAsync($"/api/cases/{caseId}/documents/{customerDoc!.Id}");
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenDelete.StatusCode);

        var lawyerUpload = await UploadCaseDocumentAsync(lawyer, caseId, "lawyer.pdf", "application/pdf");
        var lawyerDoc = await lawyerUpload.Content.ReadFromJsonAsync<CaseDocumentDto>(LawConnectWebApplicationFactory.JsonOptions);
        Assert.NotNull(lawyerDoc);

        // The case's lawyer may delete a document they did not upload themselves.
        var lawyerDeletesCustomerDoc = await lawyer.DeleteAsync($"/api/cases/{caseId}/documents/{customerDoc.Id}");
        Assert.True(lawyerDeletesCustomerDoc.IsSuccessStatusCode, await lawyerDeletesCustomerDoc.Content.ReadAsStringAsync());

        // The uploader may delete their own document.
        var uploaderDeletesOwnDoc = await lawyer.DeleteAsync($"/api/cases/{caseId}/documents/{lawyerDoc!.Id}");
        Assert.True(uploaderDeletesOwnDoc.IsSuccessStatusCode, await uploaderDeletesOwnDoc.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task CaseRating_HappyPath_OnConcludedCase_Succeeds_AndDuplicateIsRejected()
    {
        var (lawyer, _, customer, _) = await SetUpRelationshipAsync(UniquePhone("18"), UniquePhone("19"), "BAR-CM-0013");
        var caseId = await CreateCaseAsync(lawyer, customer.CustomerId);

        var close = await lawyer.PostAsJsonAsync($"/api/cases/{caseId}/status", new { status = CaseStatus.Closed, note = (string?)null });
        Assert.True(close.IsSuccessStatusCode, await close.Content.ReadAsStringAsync());

        var rate = await customer.Client.PostAsJsonAsync($"/api/cases/{caseId}/rating", new { stars = 5, reviewText = "Excellent handling of the matter." });
        Assert.True(rate.IsSuccessStatusCode, await rate.Content.ReadAsStringAsync());
        var ratingBody = await rate.Content.ReadFromJsonAsync<CaseRatingDto>(LawConnectWebApplicationFactory.JsonOptions);
        Assert.NotNull(ratingBody);
        Assert.Equal(5, ratingBody!.Stars);

        var detail = await customer.Client.GetFromJsonAsync<CaseDetailDto>($"/api/cases/{caseId}", LawConnectWebApplicationFactory.JsonOptions);
        Assert.NotNull(detail!.Rating);
        Assert.Equal(5, detail.Rating!.Stars);

        var duplicate = await customer.Client.PostAsJsonAsync($"/api/cases/{caseId}/rating", new { stars = 3, reviewText = (string?)null });
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
    }

    [Fact]
    public async Task CaseRating_OnActiveCase_Rejected()
    {
        var (lawyer, _, customer, _) = await SetUpRelationshipAsync(UniquePhone("20"), UniquePhone("21"), "BAR-CM-0014");
        var caseId = await CreateCaseAsync(lawyer, customer.CustomerId);

        var rate = await customer.Client.PostAsJsonAsync($"/api/cases/{caseId}/rating", new { stars = 4, reviewText = (string?)null });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, rate.StatusCode);
    }

    [Fact]
    public async Task CaseRating_LawyerCannotSubmit_PolicyRejectsAtTheDoor()
    {
        var (lawyer, _, customer, _) = await SetUpRelationshipAsync(UniquePhone("22"), UniquePhone("23"), "BAR-CM-0015");
        var caseId = await CreateCaseAsync(lawyer, customer.CustomerId);

        var close = await lawyer.PostAsJsonAsync($"/api/cases/{caseId}/status", new { status = CaseStatus.Closed, note = (string?)null });
        Assert.True(close.IsSuccessStatusCode, await close.Content.ReadAsStringAsync());

        var rate = await lawyer.PostAsJsonAsync($"/api/cases/{caseId}/rating", new { stars = 5, reviewText = (string?)null });
        Assert.Equal(HttpStatusCode.Forbidden, rate.StatusCode);
    }

    [Fact]
    public async Task TrustScore_StallingDeduction_PopulatesExplanatoryNote_AndFloorsCaseManagementAtZero()
    {
        var (lawyer, lawyerId, customer, _) = await SetUpRelationshipAsync(UniquePhone("24"), UniquePhone("25"), "BAR-CM-0016");

        var caseIds = new List<Guid>();
        for (var i = 0; i < 3; i++)
        {
            caseIds.Add(await CreateCaseAsync(lawyer, customer.CustomerId));
        }

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            foreach (var id in caseIds)
            {
                var entity = await db.Cases.FirstAsync(c => c.Id == id);
                entity.CreatedAt = DateTime.UtcNow.AddDays(-45); // well past the 30-day severe threshold
            }

            await db.SaveChangesAsync();
        }

        using (var scope = _factory.Services.CreateScope())
        {
            var trustScoreService = scope.ServiceProvider.GetRequiredService<ITrustScoreService>();
            await trustScoreService.RecalculateAsync(lawyerId);
        }

        var trustScore = await _factory.CreateClient().GetFromJsonAsync<LawyerTrustScoreDto>(
            $"/api/lawyers/{lawyerId}/trust-score", LawConnectWebApplicationFactory.JsonOptions);
        Assert.NotNull(trustScore);
        Assert.NotNull(trustScore!.CaseManagementNote);
        Assert.Contains("more than 30 days", trustScore.CaseManagementNote);
        Assert.Equal(0, trustScore.Breakdown.CaseManagement);
    }

    [Fact]
    public async Task Admin_CanFilterCasesByStallingOnly_AndSeesActivitySummaryDeduction()
    {
        var (lawyer, lawyerId, customer, admin) = await SetUpRelationshipAsync(UniquePhone("26"), UniquePhone("27"), "BAR-CM-0017");
        var stalledCaseId = await CreateCaseAsync(lawyer, customer.CustomerId);
        var healthyCaseId = await CreateCaseAsync(lawyer, customer.CustomerId);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var entity = await db.Cases.FirstAsync(c => c.Id == stalledCaseId);
            entity.CreatedAt = DateTime.UtcNow.AddDays(-15); // Stalling, not yet severe
            await db.SaveChangesAsync();
        }

        var stallingList = await admin.GetFromJsonAsync<PagedDto<AdminCaseListItemDto>>(
            $"/api/admin/cases?stallingOnly=true", LawConnectWebApplicationFactory.JsonOptions);
        Assert.NotNull(stallingList);
        Assert.Contains(stallingList!.Items, c => c.Id == stalledCaseId);
        Assert.DoesNotContain(stallingList.Items, c => c.Id == healthyCaseId);

        using (var scope = _factory.Services.CreateScope())
        {
            var trustScoreService = scope.ServiceProvider.GetRequiredService<ITrustScoreService>();
            await trustScoreService.RecalculateAsync(lawyerId);
        }

        var activity = await admin.GetFromJsonAsync<CaseActivitySummaryDto>(
            $"/api/admin/lawyers/{lawyerId}/case-activity", LawConnectWebApplicationFactory.JsonOptions);
        Assert.NotNull(activity);
        Assert.True(activity!.StallingCaseCount >= 1);
        Assert.True(activity.TrustScoreDeduction > 0);
    }

    // ---- shared arrange helpers ----

    private async Task<(HttpClient LawyerClient, Guid LawyerId, (HttpClient Client, Guid CustomerId) Customer, HttpClient Admin)> SetUpRelationshipAsync(
        string lawyerPhone, string customerPhone, string barNumber)
    {
        var (lawyer, lawyerId, admin) = await RegisterAndApproveLawyerAsync(lawyerPhone, barNumber);
        var (customerClient, _) = await _factory.CreateAuthenticatedClientAsync(customerPhone);
        await BookAppointmentAsync(customerClient, lawyerId);
        var customerId = await GetCustomerIdAsync(customerClient);
        return (lawyer, lawyerId, (customerClient, customerId), admin);
    }

    private async Task<(HttpClient LawyerClient, Guid LawyerId, HttpClient Admin)> RegisterAndApproveLawyerAsync(string phone, string barNumber)
    {
        var client = _factory.CreateClient();
        var email = $"lawyer{Guid.NewGuid():N}@example.com";
        var register = await client.PostAsJsonAsync(
            "/api/auth/register/lawyer",
            new
            {
                fullName = "Case Management Test Lawyer",
                email,
                phoneNumber = phone,
                password = "Password1!",
                deviceIdentifier = "test-device-cm",
                platform = DevicePlatform.Android
            });
        Assert.True(register.IsSuccessStatusCode, await register.Content.ReadAsStringAsync());
        var login = await register.Content.ReadFromJsonAsync<LoginDto>(LawConnectWebApplicationFactory.JsonOptions);
        Assert.NotNull(login);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login!.Tokens.AccessToken);

        var save = await client.PutAsJsonAsync(
            "/api/lawyers/me/application",
            new
            {
                stateBarCouncil = "Karnataka State Bar Council",
                barCouncilNumber = barNumber,
                specializationIds = new[] { SeedIds.Specializations.CriminalLaw },
                yearsOfExperience = 7,
                languages = "English, Hindi",
                education = "NLSIU Bangalore",
                consultationFee = 2000m,
                cityName = "Bengaluru",
                stateName = "Karnataka",
                countryName = "India",
                bio = "Case management integration test lawyer.",
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

        await UploadDocumentAsync(client, DocumentType.BarCouncilCertificate, "bar.pdf");
        await UploadDocumentAsync(client, DocumentType.GovernmentId, "id.pdf");

        var submit = await client.PostAsync("/api/lawyers/me/application/submit", null);
        Assert.True(submit.IsSuccessStatusCode, await submit.Content.ReadAsStringAsync());
        var profile = await submit.Content.ReadFromJsonAsync<LawyerMini>(LawConnectWebApplicationFactory.JsonOptions);
        Assert.NotNull(profile);

        var admin = await CreateAdminClientAsync(UniquePhone("64"));
        var approve = await admin.PostAsync($"/api/admin/lawyers/{profile!.LawyerId}/approve", null);
        Assert.True(approve.IsSuccessStatusCode, await approve.Content.ReadAsStringAsync());

        return (client, profile.LawyerId, admin);
    }

    private static async Task BookAppointmentAsync(HttpClient customer, Guid lawyerId)
    {
        var slot = NextMondaySlotUtc();
        var book = await customer.PostAsJsonAsync(
            "/api/appointments",
            new { lawyerId, startUtc = slot, endUtc = slot.AddMinutes(30), timeZoneId = "Asia/Kolkata" });
        Assert.True(book.IsSuccessStatusCode, await book.Content.ReadAsStringAsync());
    }

    private async Task<Guid> GetCustomerIdAsync(HttpClient customerClient)
    {
        var me = await customerClient.GetFromJsonAsync<CurrentUserDto>("/api/auth/me", LawConnectWebApplicationFactory.JsonOptions);
        Assert.NotNull(me);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var customer = await db.Customers.FirstOrDefaultAsync(c => c.UserId == me!.UserId);
        if (customer is null)
        {
            // OTP login does not eagerly create a Customer row — only real customer-scoped actions
            // (e.g. booking) do, via ICurrentCustomerService.GetOrCreateAsync. Tests that need a
            // customer id without any such action create the row directly.
            customer = new Customer { UserId = me!.UserId };
            db.Customers.Add(customer);
            await db.SaveChangesAsync();
        }

        return customer.Id;
    }

    private static async Task<Guid> CreateCaseAsync(HttpClient lawyer, Guid customerId)
    {
        var create = await lawyer.PostAsJsonAsync("/api/cases", NewCaseRequest(customerId));
        Assert.True(create.IsSuccessStatusCode, await create.Content.ReadAsStringAsync());
        var body = await create.Content.ReadFromJsonAsync<CaseDto>(LawConnectWebApplicationFactory.JsonOptions);
        Assert.NotNull(body);
        return body!.Id;
    }

    private static object NewCaseRequest(Guid customerId) => new
    {
        customerId,
        specializationId = SeedIds.Specializations.CriminalLaw,
        title = "State vs. Test Matter",
        caseNumber = (string?)null,
        description = "Integration test case.",
        courtName = "District Court",
        priority = CasePriority.Medium,
        startDate = DateTime.UtcNow.Date,
        sourceAppointmentId = (Guid?)null
    };

    private static async Task<HttpResponseMessage> UploadCaseDocumentAsync(HttpClient client, Guid caseId, string fileName, string contentType)
    {
        using var form = new MultipartFormDataContent();
        var bytes = Encoding.UTF8.GetBytes("test file content");
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        form.Add(file, "file", fileName);
        return await client.PostAsync($"/api/cases/{caseId}/documents", form);
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

    private static string UniquePhone(string prefix) => $"+92{prefix}{Random.Shared.Next(1000000, 9999999)}";

    private static DateTime NextMondaySlotUtc()
    {
        var today = DateTime.UtcNow.Date;
        var daysUntilMonday = ((int)DayOfWeek.Monday - (int)today.DayOfWeek + 7) % 7;
        daysUntilMonday = daysUntilMonday == 0 ? 7 : daysUntilMonday;
        return today.AddDays(daysUntilMonday).AddHours(4).AddMinutes(30);
    }

    private sealed record LawyerMini(Guid LawyerId, string VerificationStatus);

    private sealed record CurrentUserDto(Guid UserId);

    private sealed record CaseDto(Guid Id, Guid LawyerId, Guid CustomerId, string Status);

    private sealed record UpcomingActionDto(string Kind, DateTime When, string Title, string? Location, string Status);

    private sealed record CaseDetailDto(CaseDto Case, UpcomingActionDto? UpcomingAction, CaseActivitySignalsDto? ActivitySignals, CaseRatingDto? Rating);

    private sealed record CaseActivitySignalsDto(
        int? DaysSinceLastUpdate,
        int? DaysSinceLastActivity,
        int OverdueMilestoneCount,
        int MissedHearingCount,
        int PendingActionsCount,
        DateTime? LastMeaningfulLawyerActivityAtUtc,
        int? DaysSinceLastMeaningfulLawyerActivity,
        string StallingStatus);

    private sealed record CaseRatingDto(int Stars, string? ReviewText, DateTime CreatedAt);

    private sealed record CaseDocumentDto(Guid Id, Guid CaseId, string FileName, Guid UploadedByUserId);

    private sealed record TrustScoreBreakdownDto(
        double ClientRating,
        double AppointmentReliability,
        double Responsiveness,
        double CaseUpdates,
        double CaseManagement,
        double VerifiedComplaints);

    private sealed record LawyerTrustScoreDto(
        int Score,
        string Level,
        double Rating,
        int ReviewCount,
        TrustScoreBreakdownDto Breakdown,
        string? CaseManagementNote);

    private sealed record HearingDto(Guid Id, Guid CaseId, string Status);

    private sealed record MilestoneDto(Guid Id, Guid CaseId, string Status);

    private sealed record TimelineEntryDto(string Kind, Guid EntityId, DateTime OccurredAtUtc, string Title, string? Description, string? Status);

    private sealed record PagedDto<T>(IReadOnlyList<T> Items, int TotalCount, int PageNumber, int PageSize);

    private sealed record AdminCaseListItemDto(Guid Id, string Title, string Status);

    private sealed record CaseActivitySummaryDto(
        Guid LawyerId,
        int TotalCases,
        int ActiveCases,
        int OverdueMilestoneCount,
        int MissedHearingCount,
        int StallingCaseCount,
        int SeverelyStallingCaseCount,
        double TrustScoreDeduction);

    private sealed record AdminCaseDetailDto(CaseDto Case);
}
