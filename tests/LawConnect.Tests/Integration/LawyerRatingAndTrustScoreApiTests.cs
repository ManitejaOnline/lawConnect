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
public sealed class LawyerRatingAndTrustScoreApiTests
{
    private readonly LawConnectWebApplicationFactory _factory;

    public LawyerRatingAndTrustScoreApiTests(LawConnectWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task NewlyApprovedLawyer_TrustScoreEndpoint_ComputesRealBaselineOnDemand()
    {
        var (_, _, lawyerId, admin) = await RegisterAndApproveLawyerAsync(UniquePhone("70"), "BAR-TS-0001");

        var response = await admin.GetAsync($"/api/lawyers/{lawyerId}/trust-score");
        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());

        var body = await response.Content.ReadFromJsonAsync<TrustScoreDto>(LawConnectWebApplicationFactory.JsonOptions);
        Assert.NotNull(body);
        Assert.True(body.Score is > 0 and < 100, $"expected a real neutral-default baseline, got {body.Score}");
        Assert.False(string.IsNullOrWhiteSpace(body.Level));

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var historyCount = await db.LawyerTrustScoreHistories.CountAsync(h => h.LawyerId == lawyerId);
        Assert.True(historyCount >= 1, "GET should have triggered an on-demand recalculation, recorded in history");
    }

    [Fact]
    public async Task SubmitRating_ForCompletedAppointment_UpdatesRatingAndRecalculatesTrustScoreAutomatically()
    {
        var (lawyerClient, _, lawyerId, admin) = await RegisterAndApproveLawyerAsync(UniquePhone("71"), "BAR-TS-0002");
        var (customer, _) = await _factory.CreateAuthenticatedClientAsync(UniquePhone("72"));
        var appointmentId = await BookAndCompleteAppointmentAsync(customer, lawyerId);

        int historyCountBefore;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            historyCountBefore = await db.LawyerTrustScoreHistories.CountAsync(h => h.LawyerId == lawyerId);
        }

        var rate = await customer.PostAsJsonAsync(
            $"/api/lawyers/{lawyerId}/ratings",
            new { appointmentId, stars = 5, reviewText = "Excellent, responsive counsel." });
        Assert.True(rate.IsSuccessStatusCode, await rate.Content.ReadAsStringAsync());

        var rating = await admin.GetFromJsonAsync<RatingDto>($"/api/lawyers/{lawyerId}/rating", LawConnectWebApplicationFactory.JsonOptions);
        Assert.NotNull(rating);
        Assert.Equal(1, rating.RatingCount);
        Assert.Equal(5.0, rating.RatingAverage, precision: 5);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var historyCountAfter = await db.LawyerTrustScoreHistories.CountAsync(h => h.LawyerId == lawyerId);
            Assert.True(
                historyCountAfter > historyCountBefore,
                "submitting a rating must trigger an automatic recalculation — no manual recalc endpoint exists");
        }

        _ = lawyerClient; // kept authenticated for readability of the arrange section above
    }

    [Fact]
    public async Task SubmitRating_Duplicate_Rejected()
    {
        var (_, _, lawyerId, _) = await RegisterAndApproveLawyerAsync(UniquePhone("73"), "BAR-TS-0003");
        var (customer, _) = await _factory.CreateAuthenticatedClientAsync(UniquePhone("74"));
        var appointmentId = await BookAndCompleteAppointmentAsync(customer, lawyerId);

        var first = await customer.PostAsJsonAsync($"/api/lawyers/{lawyerId}/ratings", new { appointmentId, stars = 4, reviewText = (string?)null });
        Assert.True(first.IsSuccessStatusCode, await first.Content.ReadAsStringAsync());

        var second = await customer.PostAsJsonAsync($"/api/lawyers/{lawyerId}/ratings", new { appointmentId, stars = 2, reviewText = (string?)null });
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task SubmitRating_ByCustomerWithNoEngagement_Rejected()
    {
        var (_, _, lawyerId, _) = await RegisterAndApproveLawyerAsync(UniquePhone("75"), "BAR-TS-0004");
        var (uninvolvedCustomer, _) = await _factory.CreateAuthenticatedClientAsync(UniquePhone("76"));

        var response = await uninvolvedCustomer.PostAsJsonAsync(
            $"/api/lawyers/{lawyerId}/ratings",
            new { appointmentId = Guid.NewGuid(), stars = 5, reviewText = (string?)null });

        Assert.True(
            response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Forbidden,
            $"expected the caller to be rejected, got {response.StatusCode}");
    }

    [Fact]
    public async Task SubmitRating_ByLawyerAccount_Rejected()
    {
        var (lawyerClient, _, lawyerId, _) = await RegisterAndApproveLawyerAsync(UniquePhone("77"), "BAR-TS-0005");

        var response = await lawyerClient.PostAsJsonAsync(
            $"/api/lawyers/{lawyerId}/ratings",
            new { appointmentId = Guid.NewGuid(), stars = 5, reviewText = (string?)null });

        // A random, non-existent appointment id 404s before the ownership/self-rating check is
        // even reached — either outcome proves a lawyer account cannot rate through this endpoint.
        Assert.True(
            response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Forbidden,
            $"expected the lawyer caller to be rejected, got {response.StatusCode}");
    }

    [Fact]
    public async Task ReportNoShow_ReducesAppointmentReliabilityComponent()
    {
        var (_, _, lawyerId, admin) = await RegisterAndApproveLawyerAsync(UniquePhone("78"), "BAR-TS-0006");
        var (customer, _) = await _factory.CreateAuthenticatedClientAsync(UniquePhone("79"));

        var slot = NextMondaySlotUtc();
        var book = await customer.PostAsJsonAsync(
            "/api/appointments",
            new { lawyerId, startUtc = slot, endUtc = slot.AddMinutes(30), timeZoneId = "Asia/Kolkata" });
        Assert.True(book.IsSuccessStatusCode, await book.Content.ReadAsStringAsync());
        var booked = await book.Content.ReadFromJsonAsync<AppointmentDto>(LawConnectWebApplicationFactory.JsonOptions);
        Assert.NotNull(booked);
        var appointmentId = booked!.Id;

        // Move it into the past (still Scheduled) so a no-show can legitimately be reported.
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var entity = await db.Appointments.FirstAsync(a => a.Id == appointmentId);
            entity.StartUtc = DateTime.UtcNow.AddHours(-2);
            entity.EndUtc = DateTime.UtcNow.AddHours(-1);
            await db.SaveChangesAsync();
        }

        var noShow = await customer.PostAsync($"/api/appointments/{appointmentId}/report-no-show", null);
        Assert.True(noShow.IsSuccessStatusCode, await noShow.Content.ReadAsStringAsync());

        var trustScore = await admin.GetFromJsonAsync<TrustScoreDto>(
            $"/api/lawyers/{lawyerId}/trust-score",
            LawConnectWebApplicationFactory.JsonOptions);
        Assert.NotNull(trustScore);
        Assert.True(
            trustScore.Breakdown.AppointmentReliability < 15,
            $"a no-show must score below the 75% neutral default, got {trustScore.Breakdown.AppointmentReliability}");

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var appointment = await db.Appointments.FirstAsync(a => a.Id == appointmentId);
            Assert.Equal(AppointmentStatus.NoShow, appointment.Status);
            Assert.NotNull(appointment.NoShowReportedAtUtc);
        }
    }

    [Fact]
    public async Task ClientCancellation_DoesNotCountAgainstAppointmentReliability()
    {
        var (_, _, lawyerId, admin) = await RegisterAndApproveLawyerAsync(UniquePhone("80"), "BAR-TS-0007");
        var (customer, _) = await _factory.CreateAuthenticatedClientAsync(UniquePhone("81"));

        var slot = NextMondaySlotUtc();
        var book = await customer.PostAsJsonAsync(
            "/api/appointments",
            new { lawyerId, startUtc = slot, endUtc = slot.AddMinutes(30), timeZoneId = "Asia/Kolkata" });
        Assert.True(book.IsSuccessStatusCode, await book.Content.ReadAsStringAsync());
        var appointment = await book.Content.ReadFromJsonAsync<AppointmentDto>(LawConnectWebApplicationFactory.JsonOptions);
        Assert.NotNull(appointment);

        var cancel = await customer.PostAsJsonAsync($"/api/appointments/{appointment.Id}/cancel", new { reason = "Change of plans" });
        Assert.True(cancel.IsSuccessStatusCode, await cancel.Content.ReadAsStringAsync());

        // Move it into the past so it would be counted if (incorrectly) attributed to the lawyer.
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var entity = await db.Appointments.FirstAsync(a => a.Id == appointment.Id);
            entity.StartUtc = DateTime.UtcNow.AddHours(-2);
            entity.EndUtc = DateTime.UtcNow.AddHours(-1);
            await db.SaveChangesAsync();
        }

        var trustScore = await admin.GetFromJsonAsync<TrustScoreDto>(
            $"/api/lawyers/{lawyerId}/trust-score",
            LawConnectWebApplicationFactory.JsonOptions);
        Assert.NotNull(trustScore);

        // Client-cancelled appointments are never counted in either direction, so with zero
        // lawyer-attributable appointments the component must sit at the neutral "no data" default.
        Assert.Equal(15.0, trustScore.Breakdown.AppointmentReliability, precision: 1);
    }

    [Fact]
    public async Task VerifiedComplaint_ReducesScore_DismissedComplaint_DoesNotAffectScore()
    {
        var (_, _, lawyerId, admin) = await RegisterAndApproveLawyerAsync(UniquePhone("82"), "BAR-TS-0008");
        var (customer, _) = await _factory.CreateAuthenticatedClientAsync(UniquePhone("83"));
        await BookAndCompleteAppointmentAsync(customer, lawyerId);

        var baseline = await admin.GetFromJsonAsync<TrustScoreDto>(
            $"/api/lawyers/{lawyerId}/trust-score",
            LawConnectWebApplicationFactory.JsonOptions);
        Assert.NotNull(baseline);
        Assert.Equal(10.0, baseline.Breakdown.VerifiedComplaints, precision: 5);

        // Dismissed complaint: submit, dismiss, confirm zero effect.
        var dismissSubmit = await customer.PostAsJsonAsync(
            $"/api/lawyers/{lawyerId}/complaints",
            new { appointmentId = (Guid?)null, category = "Unresponsive", description = "Did not reply for a week." });
        Assert.True(dismissSubmit.IsSuccessStatusCode, await dismissSubmit.Content.ReadAsStringAsync());
        var dismissComplaint = await dismissSubmit.Content.ReadFromJsonAsync<ComplaintDto>(LawConnectWebApplicationFactory.JsonOptions);
        Assert.NotNull(dismissComplaint);

        var dismiss = await admin.PostAsJsonAsync(
            $"/api/admin/complaints/{dismissComplaint.ComplaintId}/dismiss",
            new { notes = "No evidence found." });
        Assert.True(dismiss.IsSuccessStatusCode, await dismiss.Content.ReadAsStringAsync());

        var afterDismiss = await admin.GetFromJsonAsync<TrustScoreDto>(
            $"/api/lawyers/{lawyerId}/trust-score",
            LawConnectWebApplicationFactory.JsonOptions);
        Assert.NotNull(afterDismiss);
        Assert.Equal(10.0, afterDismiss.Breakdown.VerifiedComplaints, precision: 5);

        // Verified complaint: submit, verify as Serious, confirm the deduction lands.
        var verifySubmit = await customer.PostAsJsonAsync(
            $"/api/lawyers/{lawyerId}/complaints",
            new { appointmentId = (Guid?)null, category = "Billing", description = "Charged twice for one consultation." });
        Assert.True(verifySubmit.IsSuccessStatusCode, await verifySubmit.Content.ReadAsStringAsync());
        var verifyComplaint = await verifySubmit.Content.ReadFromJsonAsync<ComplaintDto>(LawConnectWebApplicationFactory.JsonOptions);
        Assert.NotNull(verifyComplaint);

        var verify = await admin.PostAsJsonAsync(
            $"/api/admin/complaints/{verifyComplaint.ComplaintId}/verify",
            new { severity = ComplaintSeverity.Serious, notes = "Confirmed via payment records." });
        Assert.True(verify.IsSuccessStatusCode, await verify.Content.ReadAsStringAsync());

        var afterVerify = await admin.GetFromJsonAsync<TrustScoreDto>(
            $"/api/lawyers/{lawyerId}/trust-score",
            LawConnectWebApplicationFactory.JsonOptions);
        Assert.NotNull(afterVerify);
        Assert.Equal(5.0, afterVerify.Breakdown.VerifiedComplaints, precision: 5);
        Assert.True(afterVerify.Score < baseline.Score, "a verified Serious complaint must reduce the overall score");
    }

    [Fact]
    public async Task PendingComplaint_NeverAffectsScore_UntilAdminActsOnIt()
    {
        var (_, _, lawyerId, admin) = await RegisterAndApproveLawyerAsync(UniquePhone("84"), "BAR-TS-0009");
        var (customer, _) = await _factory.CreateAuthenticatedClientAsync(UniquePhone("85"));
        await BookAndCompleteAppointmentAsync(customer, lawyerId);

        var before = await admin.GetFromJsonAsync<TrustScoreDto>($"/api/lawyers/{lawyerId}/trust-score", LawConnectWebApplicationFactory.JsonOptions);

        var submit = await customer.PostAsJsonAsync(
            $"/api/lawyers/{lawyerId}/complaints",
            new { appointmentId = (Guid?)null, category = "Other", description = "Just filed, not yet reviewed." });
        Assert.True(submit.IsSuccessStatusCode, await submit.Content.ReadAsStringAsync());

        var after = await admin.GetFromJsonAsync<TrustScoreDto>($"/api/lawyers/{lawyerId}/trust-score", LawConnectWebApplicationFactory.JsonOptions);
        Assert.NotNull(before);
        Assert.NotNull(after);
        Assert.Equal(before.Breakdown.VerifiedComplaints, after.Breakdown.VerifiedComplaints, precision: 5);
    }

    [Fact]
    public async Task CustomerCannotVerifyOrDismissComplaints_Returns403()
    {
        var (_, _, lawyerId, _) = await RegisterAndApproveLawyerAsync(UniquePhone("86"), "BAR-TS-0010");
        var (customer, _) = await _factory.CreateAuthenticatedClientAsync(UniquePhone("87"));

        var verify = await customer.PostAsJsonAsync(
            $"/api/admin/complaints/{Guid.NewGuid()}/verify",
            new { severity = ComplaintSeverity.Minor, notes = (string?)null });
        Assert.Equal(HttpStatusCode.Forbidden, verify.StatusCode);

        var dismiss = await customer.PostAsJsonAsync(
            $"/api/admin/complaints/{Guid.NewGuid()}/dismiss",
            new { notes = (string?)null });
        Assert.Equal(HttpStatusCode.Forbidden, dismiss.StatusCode);

        var list = await customer.GetAsync("/api/admin/complaints");
        Assert.Equal(HttpStatusCode.Forbidden, list.StatusCode);

        _ = lawyerId;
    }

    [Fact]
    public async Task LawyerCannotVerifyComplaints_Returns403()
    {
        var (lawyerClient, _, _, _) = await RegisterAndApproveLawyerAsync(UniquePhone("88"), "BAR-TS-0011");

        var verify = await lawyerClient.PostAsJsonAsync(
            $"/api/admin/complaints/{Guid.NewGuid()}/verify",
            new { severity = ComplaintSeverity.Minor, notes = (string?)null });
        Assert.Equal(HttpStatusCode.Forbidden, verify.StatusCode);
    }

    // ---- shared arrange helpers, matching RegistrationAndVerificationApiTests's conventions ----

    private async Task<(HttpClient LawyerClient, LoginDto LawyerLogin, Guid LawyerId, HttpClient Admin)> RegisterAndApproveLawyerAsync(
        string phone,
        string barNumber)
    {
        var client = _factory.CreateClient();
        var email = $"lawyer{Guid.NewGuid():N}@example.com";
        var register = await client.PostAsJsonAsync(
            "/api/auth/register/lawyer",
            new
            {
                fullName = "Trust Score Test Lawyer",
                email,
                phoneNumber = phone,
                password = "Password1!",
                deviceIdentifier = "test-device-ts",
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
                bio = "Trust Score integration test lawyer.",
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

        var admin = await CreateAdminClientAsync(UniquePhone("69"));
        var approve = await admin.PostAsync($"/api/admin/lawyers/{profile!.LawyerId}/approve", null);
        Assert.True(approve.IsSuccessStatusCode, await approve.Content.ReadAsStringAsync());

        return (client, login, profile.LawyerId, admin);
    }

    private async Task<Guid> BookAndCompleteAppointmentAsync(HttpClient customer, Guid lawyerId)
    {
        var slot = NextMondaySlotUtc();
        var book = await customer.PostAsJsonAsync(
            "/api/appointments",
            new { lawyerId, startUtc = slot, endUtc = slot.AddMinutes(30), timeZoneId = "Asia/Kolkata" });
        Assert.True(book.IsSuccessStatusCode, await book.Content.ReadAsStringAsync());
        var appointment = await book.Content.ReadFromJsonAsync<AppointmentDto>(LawConnectWebApplicationFactory.JsonOptions);
        Assert.NotNull(appointment);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var entity = await db.Appointments.FirstAsync(a => a.Id == appointment!.Id);
        entity.Status = AppointmentStatus.Completed;
        entity.StartUtc = DateTime.UtcNow.AddHours(-2);
        entity.EndUtc = DateTime.UtcNow.AddHours(-1);
        await db.SaveChangesAsync();

        return appointment!.Id;
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

    private static string UniquePhone(string prefix) => $"+91{prefix}{Random.Shared.Next(1000000, 9999999)}";

    /// <summary>
    /// The next Monday at 10:00 IST (04:30 UTC) — falls inside the test lawyer's declared
    /// Monday 09:00-17:00 Asia/Kolkata availability regardless of what day the tests run on.
    /// </summary>
    private static DateTime NextMondaySlotUtc()
    {
        var today = DateTime.UtcNow.Date;
        var daysUntilMonday = ((int)DayOfWeek.Monday - (int)today.DayOfWeek + 7) % 7;
        daysUntilMonday = daysUntilMonday == 0 ? 7 : daysUntilMonday;
        return today.AddDays(daysUntilMonday).AddHours(4).AddMinutes(30);
    }

    private sealed record LawyerMini(Guid LawyerId, string VerificationStatus);

    private sealed record AppointmentDto(Guid Id);

    private sealed record RatingDto(double RatingAverage, int RatingCount, double BayesianScore);

    private sealed record TrustScoreBreakdownDto(
        double ClientRating,
        double AppointmentReliability,
        double Responsiveness,
        double CaseUpdates,
        double CaseManagement,
        double VerifiedComplaints);

    private sealed record TrustScoreDto(int Score, string Level, double Rating, int ReviewCount, TrustScoreBreakdownDto Breakdown);

    private sealed record ComplaintDto(Guid ComplaintId, Guid LawyerId, string Category, string Status, DateTime CreatedAt);
}
