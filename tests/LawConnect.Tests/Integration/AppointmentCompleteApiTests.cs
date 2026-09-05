using System.Net;
using System.Net.Http.Json;
using LawConnect.Domain.Enums;
using LawConnect.Infrastructure.Persistence;
using LawConnect.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LawConnect.Tests.Integration;

[Collection("Integration")]
public sealed class AppointmentCompleteApiTests
{
    private readonly LawConnectWebApplicationFactory _factory;

    public AppointmentCompleteApiTests(LawConnectWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Lawyer_CanComplete_AfterStart_ThenCustomerCanRate()
    {
        var (lawyerClient, lawyerId, customerClient, appointmentId) = await BookScheduledAppointmentAsync();
        await ShiftAppointmentIntoThePastAsync(appointmentId);

        var complete = await lawyerClient.PostAsync($"/api/appointments/{appointmentId}/complete", null);
        Assert.True(complete.IsSuccessStatusCode, await complete.Content.ReadAsStringAsync());
        var completed = await complete.Content.ReadFromJsonAsync<AppointmentDto>(
            LawConnectWebApplicationFactory.JsonOptions);
        Assert.Equal("Completed", completed?.Status);

        var again = await lawyerClient.PostAsync($"/api/appointments/{appointmentId}/complete", null);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, again.StatusCode);

        var rating = await customerClient.PostAsJsonAsync(
            $"/api/lawyers/{lawyerId}/ratings",
            new { appointmentId, stars = 5, reviewText = "Clear advice" });
        Assert.True(rating.IsSuccessStatusCode, await rating.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Complete_BeforeStart_IsRejected()
    {
        var (lawyerClient, _, _, appointmentId) = await BookScheduledAppointmentAsync();

        var response = await lawyerClient.PostAsync($"/api/appointments/{appointmentId}/complete", null);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Customer_CannotComplete_Appointment()
    {
        var (_, _, customerClient, appointmentId) = await BookScheduledAppointmentAsync();
        await ShiftAppointmentIntoThePastAsync(appointmentId);

        var response = await customerClient.PostAsync($"/api/appointments/{appointmentId}/complete", null);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task OtherLawyer_CannotComplete_Appointment()
    {
        var (_, _, _, appointmentId) = await BookScheduledAppointmentAsync();
        await ShiftAppointmentIntoThePastAsync(appointmentId);

        var otherPhone = UniquePhone("01");
        var (otherLawyer, _) = await _factory.CreateAuthenticatedClientAsync(otherPhone);
        var register = await otherLawyer.PostAsJsonAsync(
            "/api/lawyers/register",
            new
            {
                barCouncilNumber = $"BAR-COMPLETE-OTHER-{Random.Shared.Next(10000, 99999)}",
                bio = "Other lawyer",
                yearsOfExperience = 4,
                consultationFee = 1500m,
                emergencyFee = 2000m,
                specializationIds = new[] { SeedIds.Specializations.CivilLaw }
            });
        Assert.True(register.IsSuccessStatusCode, await register.Content.ReadAsStringAsync());
        var profile = await register.Content.ReadFromJsonAsync<LawyerProfileDto>(
            LawConnectWebApplicationFactory.JsonOptions);
        Assert.NotNull(profile);
        await _factory.ApproveLawyerAsync(profile.LawyerId);
        (otherLawyer, _) = await _factory.CreateAuthenticatedClientAsync(otherPhone);

        var response = await otherLawyer.PostAsync($"/api/appointments/{appointmentId}/complete", null);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private async Task<(HttpClient Lawyer, Guid LawyerId, HttpClient Customer, Guid AppointmentId)>
        BookScheduledAppointmentAsync()
    {
        var lawyerPhone = UniquePhone("10");
        var (lawyerClient, _) = await _factory.CreateAuthenticatedClientAsync(lawyerPhone);
        var register = await lawyerClient.PostAsJsonAsync(
            "/api/lawyers/register",
            new
            {
                barCouncilNumber = $"BAR-COMPLETE-{Random.Shared.Next(10000, 99999)}",
                bio = "Complete-flow lawyer",
                yearsOfExperience = 8,
                consultationFee = 2000m,
                emergencyFee = 3500m,
                specializationIds = new[] { SeedIds.Specializations.CivilLaw }
            });
        Assert.True(register.IsSuccessStatusCode, await register.Content.ReadAsStringAsync());
        var lawyerProfile = await register.Content.ReadFromJsonAsync<LawyerProfileDto>(
            LawConnectWebApplicationFactory.JsonOptions);
        Assert.NotNull(lawyerProfile);
        await _factory.ApproveLawyerAsync(lawyerProfile.LawyerId);
        (lawyerClient, _) = await _factory.CreateAuthenticatedClientAsync(lawyerPhone);

        var targetDate = NextDateFor(DayOfWeek.Monday);
        var availability = await lawyerClient.PutAsJsonAsync(
            "/api/lawyers/me/availability",
            new
            {
                slots = new[]
                {
                    new
                    {
                        dayOfWeek = DayOfWeekFlag.Monday,
                        startTime = "09:00:00",
                        endTime = "12:00:00",
                        isAvailable = true,
                        timeZoneId = "Asia/Kolkata"
                    }
                }
            });
        Assert.True(availability.IsSuccessStatusCode, await availability.Content.ReadAsStringAsync());

        var slots = await lawyerClient.GetFromJsonAsync<SlotDto[]>(
            $"/api/lawyers/{lawyerProfile.LawyerId}/slots?date={targetDate:yyyy-MM-dd}&timeZoneId=Asia/Kolkata&slotMinutes=30",
            LawConnectWebApplicationFactory.JsonOptions);
        Assert.NotNull(slots);
        Assert.NotEmpty(slots);

        var (customerClient, _) = await _factory.CreateAuthenticatedClientAsync(UniquePhone("11"));
        var slot = slots[0];
        var book = await customerClient.PostAsJsonAsync(
            "/api/appointments",
            new
            {
                lawyerId = lawyerProfile.LawyerId,
                startUtc = slot.StartUtc,
                endUtc = slot.EndUtc,
                notes = "Consult",
                timeZoneId = "Asia/Kolkata"
            });
        Assert.True(book.IsSuccessStatusCode, await book.Content.ReadAsStringAsync());
        var appointment = await book.Content.ReadFromJsonAsync<AppointmentDto>(
            LawConnectWebApplicationFactory.JsonOptions);
        Assert.NotNull(appointment);
        return (lawyerClient, lawyerProfile.LawyerId, customerClient, appointment.Id);
    }

    private async Task ShiftAppointmentIntoThePastAsync(Guid appointmentId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var entity = await db.Appointments.FirstAsync(a => a.Id == appointmentId);
        entity.StartUtc = DateTime.UtcNow.AddHours(-2);
        entity.EndUtc = DateTime.UtcNow.AddHours(-1);
        await db.SaveChangesAsync();
    }

    private static string UniquePhone(string prefix) => $"+91{prefix}{Random.Shared.Next(1000000, 9999999)}";

    private static DateOnly NextDateFor(DayOfWeek dayOfWeek)
    {
        var date = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(1));
        while (date.DayOfWeek != dayOfWeek)
        {
            date = date.AddDays(1);
        }

        return date;
    }

    private sealed record LawyerProfileDto(Guid LawyerId);

    private sealed record SlotDto(DateTime StartUtc, DateTime EndUtc, string TimeZoneId);

    private sealed record AppointmentDto(Guid Id, string Status);
}
