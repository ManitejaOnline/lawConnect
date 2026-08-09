using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using LawConnect.Application.Features.Payments.Services;
using LawConnect.Domain.Enums;
using LawConnect.Infrastructure.Payments;
using LawConnect.Infrastructure.Persistence;
using LawConnect.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LawConnect.Tests.Integration;

[Collection("Integration")]
public sealed class PaymentsApiTests
{
    private readonly LawConnectWebApplicationFactory _factory;

    public PaymentsApiTests(LawConnectWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AppointmentPayConfirmRefund_AndDuplicateIdempotency_Succeed()
    {
        var (lawyerClient, _) = await _factory.CreateAuthenticatedClientAsync("+919720000001");
        var register = await lawyerClient.PostAsJsonAsync(
            "/api/lawyers/register",
            new
            {
                barCouncilNumber = "BAR-PAY-1001",
                bio = "Pay lawyer",
                yearsOfExperience = 5,
                consultationFee = 1500m,
                emergencyFee = 2500m,
                specializationIds = new[] { SeedIds.Specializations.CivilLaw }
            });
        Assert.True(register.IsSuccessStatusCode, await register.Content.ReadAsStringAsync());
        var lawyer = await register.Content.ReadFromJsonAsync<LawyerMiniDto>(LawConnectWebApplicationFactory.JsonOptions);
        Assert.NotNull(lawyer);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var entity = await db.Lawyers.FirstAsync(l => l.Id == lawyer.LawyerId);
            entity.IsVerified = true;
            entity.IsOnline = true;
            entity.IsAvailableForEmergency = true;
            await db.SaveChangesAsync();
        }

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

        var (customer, _) = await _factory.CreateAuthenticatedClientAsync("+919720000002");
        var targetDate = NextMonday();
        var slots = await customer.GetFromJsonAsync<SlotMiniDto[]>(
            $"/api/lawyers/{lawyer.LawyerId}/slots?date={targetDate:yyyy-MM-dd}&timeZoneId=Asia/Kolkata&slotMinutes=30",
            LawConnectWebApplicationFactory.JsonOptions);
        Assert.NotNull(slots);
        Assert.NotEmpty(slots);

        var book = await customer.PostAsJsonAsync(
            "/api/appointments",
            new
            {
                lawyerId = lawyer.LawyerId,
                startUtc = slots[0].StartUtc,
                endUtc = slots[0].EndUtc,
                notes = "Paid consult",
                timeZoneId = "Asia/Kolkata"
            });
        Assert.True(book.IsSuccessStatusCode, await book.Content.ReadAsStringAsync());
        var appointment = await book.Content.ReadFromJsonAsync<AppointmentMiniDto>(
            LawConnectWebApplicationFactory.JsonOptions);
        Assert.NotNull(appointment);

        var initiate = await customer.PostAsJsonAsync(
            $"/api/payments/appointments/{appointment.Id}/initiate",
            new { idempotencyKey = "appt-pay-1" });
        Assert.True(initiate.IsSuccessStatusCode, await initiate.Content.ReadAsStringAsync());
        var payment = await initiate.Content.ReadFromJsonAsync<PaymentDto>(LawConnectWebApplicationFactory.JsonOptions);
        Assert.NotNull(payment);
        Assert.Equal(nameof(PaymentStatus.Pending), payment.Status);

        var initiateDup = await customer.PostAsJsonAsync(
            $"/api/payments/appointments/{appointment.Id}/initiate",
            new { idempotencyKey = "appt-pay-1" });
        Assert.True(initiateDup.IsSuccessStatusCode, await initiateDup.Content.ReadAsStringAsync());
        var paymentDup = await initiateDup.Content.ReadFromJsonAsync<PaymentDto>(LawConnectWebApplicationFactory.JsonOptions);
        Assert.NotNull(paymentDup);
        Assert.Equal(payment.PaymentId, paymentDup.PaymentId);

        var providerPaymentId = $"pay_{Guid.NewGuid():N}";
        var badSignature = await customer.PostAsJsonAsync(
            $"/api/payments/{payment.PaymentId}/confirm",
            new
            {
                providerPaymentId = "pay_other",
                signature = "bad-signature",
                idempotencyKey = "appt-confirm-bad"
            });
        Assert.False(badSignature.IsSuccessStatusCode);

        var signature = FakePaymentProvider.ComputeSignature(payment.ProviderOrderId!, providerPaymentId);
        var confirm = await customer.PostAsJsonAsync(
            $"/api/payments/{payment.PaymentId}/confirm",
            new
            {
                providerPaymentId,
                signature,
                idempotencyKey = "appt-confirm-1"
            });
        Assert.True(confirm.IsSuccessStatusCode, await confirm.Content.ReadAsStringAsync());
        var captured = await confirm.Content.ReadFromJsonAsync<PaymentDto>(LawConnectWebApplicationFactory.JsonOptions);
        Assert.NotNull(captured);
        Assert.Equal(nameof(PaymentStatus.Captured), captured.Status);

        var invoice = await customer.GetFromJsonAsync<InvoiceDto>(
            $"/api/payments/{payment.PaymentId}/invoice",
            LawConnectWebApplicationFactory.JsonOptions);
        Assert.NotNull(invoice);
        Assert.False(string.IsNullOrWhiteSpace(invoice.InvoiceNumber));

        var refund = await customer.PostAsJsonAsync(
            $"/api/payments/{payment.PaymentId}/refund",
            new { amount = (decimal?)null, reason = "Customer cancelled", idempotencyKey = "appt-refund-1" });
        Assert.True(refund.IsSuccessStatusCode, await refund.Content.ReadAsStringAsync());
        var refundDto = await refund.Content.ReadFromJsonAsync<RefundDto>(LawConnectWebApplicationFactory.JsonOptions);
        Assert.NotNull(refundDto);
        Assert.Equal(nameof(RefundStatus.Succeeded), refundDto.Status);

        var refundDup = await customer.PostAsJsonAsync(
            $"/api/payments/{payment.PaymentId}/refund",
            new { amount = (decimal?)null, reason = "Customer cancelled", idempotencyKey = "appt-refund-1" });
        Assert.True(refundDup.IsSuccessStatusCode, await refundDup.Content.ReadAsStringAsync());
        var refundDupDto = await refundDup.Content.ReadFromJsonAsync<RefundDto>(LawConnectWebApplicationFactory.JsonOptions);
        Assert.NotNull(refundDupDto);
        Assert.Equal(refundDto.RefundId, refundDupDto.RefundId);
    }

    [Fact]
    public async Task Webhook_DuplicateAndInvalidSignature_AreHandled()
    {
        var client = _factory.CreateClient();
        var bodyObj = new
        {
            event_id = "evt_test_1",
            @event = "payment.captured",
            payload = new
            {
                payment = new
                {
                    entity = new
                    {
                        id = "pay_wh_1",
                        order_id = "order_missing"
                    }
                }
            }
        };
        var json = JsonSerializer.Serialize(bodyObj);

        var invalid = new HttpRequestMessage(HttpMethod.Post, "/api/payments/webhooks/Fake")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        invalid.Headers.Add("X-Payment-Signature", "invalid");
        var invalidResponse = await client.SendAsync(invalid);
        Assert.False(invalidResponse.IsSuccessStatusCode);

        var valid = new HttpRequestMessage(HttpMethod.Post, "/api/payments/webhooks/Fake")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        valid.Headers.Add("X-Payment-Signature", "fake-webhook-signature");
        var validResponse = await client.SendAsync(valid);
        Assert.True(validResponse.IsSuccessStatusCode, await validResponse.Content.ReadAsStringAsync());

        var duplicate = new HttpRequestMessage(HttpMethod.Post, "/api/payments/webhooks/Fake")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        duplicate.Headers.Add("X-Payment-Signature", "fake-webhook-signature");
        var duplicateResponse = await client.SendAsync(duplicate);
        Assert.True(duplicateResponse.IsSuccessStatusCode, await duplicateResponse.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task WalletTopUp_HoldCapture_AndOutboxRetry_Succeed()
    {
        var (customer, login) = await _factory.CreateAuthenticatedClientAsync("+919720000011");
        var topUp = await customer.PostAsJsonAsync(
            "/api/payments/wallet/top-up",
            new { amount = 5000m, idempotencyKey = "wallet-top-1" });
        Assert.True(topUp.IsSuccessStatusCode, await topUp.Content.ReadAsStringAsync());
        var wallet = await topUp.Content.ReadFromJsonAsync<WalletDto>(LawConnectWebApplicationFactory.JsonOptions);
        Assert.NotNull(wallet);
        Assert.Equal(5000m, wallet.AvailableBalance);

        var topUpDup = await customer.PostAsJsonAsync(
            "/api/payments/wallet/top-up",
            new { amount = 5000m, idempotencyKey = "wallet-top-1" });
        Assert.True(topUpDup.IsSuccessStatusCode, await topUpDup.Content.ReadAsStringAsync());

        var lawyer = await CreateEmergencyLawyerAsync("+919720000012", "BAR-PAY-E1");
        var createSos = await customer.PostAsJsonAsync(
            "/api/emergency/sos",
            new
            {
                categoryId = SeedIds.EmergencyCategories.CriminalEmergency,
                latitude = 12.97,
                longitude = 77.59,
                notes = "paid sos"
            });
        Assert.True(createSos.IsSuccessStatusCode, await createSos.Content.ReadAsStringAsync());
        var sos = await createSos.Content.ReadFromJsonAsync<SosDto>(LawConnectWebApplicationFactory.JsonOptions);
        Assert.NotNull(sos);

        var incoming = await lawyer.Client.GetFromJsonAsync<IncomingDto[]>(
            "/api/emergency/incoming",
            LawConnectWebApplicationFactory.JsonOptions);
        Assert.NotNull(incoming);
        Assert.NotEmpty(incoming);

        var accept = await lawyer.Client.PostAsJsonAsync(
            $"/api/emergency/assignments/{incoming[0].AssignmentId}/accept",
            new { });
        Assert.True(accept.IsSuccessStatusCode, await accept.Content.ReadAsStringAsync());

        var walletAfterHold = await customer.GetFromJsonAsync<WalletDto>(
            "/api/payments/wallet",
            LawConnectWebApplicationFactory.JsonOptions);
        Assert.NotNull(walletAfterHold);
        Assert.True(walletAfterHold.HeldBalance > 0);

        var complete = await customer.PostAsJsonAsync($"/api/emergency/sos/{sos.RequestId}/complete", new { });
        Assert.True(complete.IsSuccessStatusCode, await complete.Content.ReadAsStringAsync());

        var walletAfterCapture = await customer.GetFromJsonAsync<WalletDto>(
            "/api/payments/wallet",
            LawConnectWebApplicationFactory.JsonOptions);
        Assert.NotNull(walletAfterCapture);
        Assert.Equal(0m, walletAfterCapture.HeldBalance);

        using var scope = _factory.Services.CreateScope();
        var outbox = scope.ServiceProvider.GetRequiredService<IOutboxProcessor>();
        await outbox.ProcessPendingAsync();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var published = await db.OutboxMessages.CountAsync(m => m.Status == OutboxMessageStatus.Published);
        Assert.True(published > 0);

        var history = await customer.GetFromJsonAsync<PagedMiniDto<TxDto>>(
            "/api/payments/wallet/transactions?pageNumber=1&pageSize=50",
            LawConnectWebApplicationFactory.JsonOptions);
        Assert.NotNull(history);
        Assert.NotEmpty(history.Items);
        _ = login;
    }

    private async Task<(HttpClient Client, Guid LawyerId)> CreateEmergencyLawyerAsync(string phone, string bar)
    {
        var (client, _) = await _factory.CreateAuthenticatedClientAsync(phone);
        var register = await client.PostAsJsonAsync(
            "/api/lawyers/register",
            new
            {
                barCouncilNumber = bar,
                bio = "Emergency pay lawyer",
                yearsOfExperience = 8,
                consultationFee = 1000m,
                emergencyFee = 2000m,
                specializationIds = new[] { SeedIds.Specializations.CriminalLaw }
            });
        Assert.True(register.IsSuccessStatusCode, await register.Content.ReadAsStringAsync());
        var profile = await register.Content.ReadFromJsonAsync<LawyerMiniDto>(LawConnectWebApplicationFactory.JsonOptions);
        Assert.NotNull(profile);

        await client.PostAsJsonAsync(
            "/api/users/me/addresses",
            new
            {
                line1 = "Office",
                line2 = (string?)null,
                postalCode = "560001",
                landmark = (string?)null,
                cityId = (Guid?)null,
                latitude = 12.97,
                longitude = 77.59,
                isPrimary = true,
                label = "Office"
            });
        await client.PutAsJsonAsync("/api/lawyers/me/online-status", new { isOnline = true });
        await client.PutAsJsonAsync(
            "/api/lawyers/me/emergency-availability",
            new { isAvailableForEmergency = true, emergencyFee = 2000m });

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var lawyer = await db.Lawyers.FindAsync(profile.LawyerId);
        Assert.NotNull(lawyer);
        lawyer.IsVerified = true;
        lawyer.IsOnline = true;
        lawyer.IsAvailableForEmergency = true;
        lawyer.EmergencyFee = 2000m;
        await db.SaveChangesAsync();

        var (reauthed, _) = await _factory.CreateAuthenticatedClientAsync(phone);
        return (reauthed, profile.LawyerId);
    }

    private static DateOnly NextMonday()
    {
        var date = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(1));
        while (date.DayOfWeek != DayOfWeek.Monday)
        {
            date = date.AddDays(1);
        }

        return date;
    }

    private sealed record LawyerMiniDto(Guid LawyerId);
    private sealed record SlotMiniDto(DateTime StartUtc, DateTime EndUtc);
    private sealed record AppointmentMiniDto(Guid Id);
    private sealed record PaymentDto(Guid PaymentId, string Status, string? ProviderOrderId);
    private sealed record RefundDto(Guid RefundId, string Status);
    private sealed record InvoiceDto(string InvoiceNumber);
    private sealed record WalletDto(Guid WalletId, decimal AvailableBalance, decimal HeldBalance);
    private sealed record SosDto(Guid RequestId);
    private sealed record IncomingDto(Guid AssignmentId);
    private sealed record TxDto(Guid TransactionId, string Type);
    private sealed record PagedMiniDto<T>(IReadOnlyList<T> Items, int TotalCount);
}
