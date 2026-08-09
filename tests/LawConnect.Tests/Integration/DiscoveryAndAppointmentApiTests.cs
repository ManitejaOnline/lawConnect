using System.Net.Http.Json;
using LawConnect.Domain.Enums;
using LawConnect.Shared.Constants;

namespace LawConnect.Tests.Integration;

[Collection("Integration")]
public sealed class DiscoveryAndAppointmentApiTests
{
    private readonly LawConnectWebApplicationFactory _factory;

    public DiscoveryAndAppointmentApiTests(LawConnectWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task SearchBookRescheduleCancel_Flow_Succeeds()
    {
        var (lawyerClient, _) = await _factory.CreateAuthenticatedClientAsync("+919900011122");

        var register = await lawyerClient.PostAsJsonAsync(
            "/api/lawyers/register",
            new
            {
                barCouncilNumber = "BAR-DISC-2001",
                bio = "Discovery test lawyer",
                yearsOfExperience = 10,
                consultationFee = 2000m,
                emergencyFee = 3500m,
                specializationIds = new[] { SeedIds.Specializations.CivilLaw }
            });
        Assert.True(register.IsSuccessStatusCode, await register.Content.ReadAsStringAsync());
        var lawyerProfile = await register.Content.ReadFromJsonAsync<LawyerProfileDto>(
            LawConnectWebApplicationFactory.JsonOptions);
        Assert.NotNull(lawyerProfile);
        await _factory.ApproveLawyerAsync(lawyerProfile.LawyerId);

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

        var search = await lawyerClient.GetFromJsonAsync<PagedDto<LawyerSearchDto>>(
            $"/api/lawyers/search?specializationId={SeedIds.Specializations.CivilLaw}&sortBy=fee&pageNumber=1&pageSize=20",
            LawConnectWebApplicationFactory.JsonOptions);
        Assert.NotNull(search);
        Assert.Contains(search.Items, x => x.LawyerId == lawyerProfile.LawyerId);

        var slots = await lawyerClient.GetFromJsonAsync<SlotDto[]>(
            $"/api/lawyers/{lawyerProfile.LawyerId}/slots?date={targetDate:yyyy-MM-dd}&timeZoneId=Asia/Kolkata&slotMinutes=30",
            LawConnectWebApplicationFactory.JsonOptions);
        Assert.NotNull(slots);
        Assert.NotEmpty(slots);

        var (customerClient, _) = await _factory.CreateAuthenticatedClientAsync("+919900011133");
        var slot = slots[0];

        var book = await customerClient.PostAsJsonAsync(
            "/api/appointments",
            new
            {
                lawyerId = lawyerProfile.LawyerId,
                startUtc = slot.StartUtc,
                endUtc = slot.EndUtc,
                notes = "Need consultation",
                timeZoneId = "Asia/Kolkata"
            });
        Assert.True(book.IsSuccessStatusCode, await book.Content.ReadAsStringAsync());
        var appointment = await book.Content.ReadFromJsonAsync<AppointmentDto>(
            LawConnectWebApplicationFactory.JsonOptions);
        Assert.NotNull(appointment);
        Assert.Equal("Scheduled", appointment.Status);

        var conflict = await customerClient.PostAsJsonAsync(
            "/api/appointments",
            new
            {
                lawyerId = lawyerProfile.LawyerId,
                startUtc = slot.StartUtc,
                endUtc = slot.EndUtc,
                notes = "Duplicate",
                timeZoneId = "Asia/Kolkata"
            });
        Assert.Equal(System.Net.HttpStatusCode.Conflict, conflict.StatusCode);

        var upcoming = await customerClient.GetFromJsonAsync<PagedDto<AppointmentDto>>(
            "/api/appointments/upcoming?pageNumber=1&pageSize=20",
            LawConnectWebApplicationFactory.JsonOptions);
        Assert.NotNull(upcoming);
        Assert.Contains(upcoming.Items, a => a.Id == appointment.Id);

        Assert.True(slots.Length >= 2, "Need a second free slot for reschedule.");
        var nextSlot = slots[1];
        var reschedule = await customerClient.PostAsJsonAsync(
            $"/api/appointments/{appointment.Id}/reschedule",
            new
            {
                startUtc = nextSlot.StartUtc,
                endUtc = nextSlot.EndUtc,
                timeZoneId = "Asia/Kolkata"
            });
        Assert.True(reschedule.IsSuccessStatusCode, await reschedule.Content.ReadAsStringAsync());

        var calendar = await customerClient.GetFromJsonAsync<AppointmentDto[]>(
            $"/api/appointments/calendar?fromUtc={targetDate.AddDays(-1):yyyy-MM-dd}T00:00:00Z&toUtc={targetDate.AddDays(2):yyyy-MM-dd}T00:00:00Z",
            LawConnectWebApplicationFactory.JsonOptions);
        Assert.NotNull(calendar);
        Assert.Contains(calendar, a => a.Id == appointment.Id);

        var cancel = await customerClient.PostAsJsonAsync(
            $"/api/appointments/{appointment.Id}/cancel",
            new { reason = "Plans changed" });
        Assert.True(cancel.IsSuccessStatusCode, await cancel.Content.ReadAsStringAsync());

        var history = await customerClient.GetFromJsonAsync<PagedDto<AppointmentDto>>(
            "/api/appointments/history?pageNumber=1&pageSize=20",
            LawConnectWebApplicationFactory.JsonOptions);
        Assert.NotNull(history);
        Assert.Contains(history.Items, a => a.Id == appointment.Id && a.Status == "Cancelled");
    }

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

    private sealed record LawyerSearchDto(Guid LawyerId, decimal ConsultationFee);

    private sealed record SlotDto(DateTime StartUtc, DateTime EndUtc, string TimeZoneId);

    private sealed record AppointmentDto(Guid Id, string Status, DateTime StartUtc, DateTime EndUtc);

    private sealed record PagedDto<T>(IReadOnlyList<T> Items, int TotalCount, int PageNumber, int PageSize);
}
