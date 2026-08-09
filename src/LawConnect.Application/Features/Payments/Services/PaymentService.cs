#pragma warning disable CS1591
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using LawConnect.Application.Common.Events;
using LawConnect.Application.Common.Interfaces;
using LawConnect.Application.Features.Payments.Events;
using LawConnect.Application.Features.Payments.Models;
using LawConnect.Domain.Entities;
using LawConnect.Domain.Enums;
using LawConnect.Domain.Exceptions;
using LawConnect.Shared.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LawConnect.Application.Features.Payments.Services;

/// <summary>
/// Orchestrates wallet, payments, refunds, invoices, outbox, and audit.
/// </summary>
public interface IPaymentService
{
    Task<Wallet> GetOrCreateWalletAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<PaymentResponse> InitiateAppointmentPaymentAsync(Guid appointmentId, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<PaymentResponse> ConfirmAppointmentPaymentAsync(Guid paymentId, string providerPaymentId, string signature, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<PaymentResponse> PlaceEmergencyHoldAsync(Guid emergencyRequestId, Guid customerUserId, decimal amount, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<PaymentResponse> CaptureEmergencyHoldAsync(Guid emergencyRequestId, string idempotencyKey, CancellationToken cancellationToken = default);
    Task ReleaseEmergencyHoldAsync(Guid emergencyRequestId, string reason, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<RefundResponse> RefundPaymentAsync(Guid paymentId, decimal? amount, string? reason, string idempotencyKey, CancellationToken cancellationToken = default);
    Task TopUpWalletAsync(Guid userId, decimal amount, string idempotencyKey, CancellationToken cancellationToken = default);
    Task ProcessWebhookAsync(string provider, string eventId, string eventType, string rawBody, string signature, CancellationToken cancellationToken = default);
    Task WriteAuditAsync(string action, string entityName, string? entityId, object? oldValues, object? newValues, Guid? userId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Default payment orchestration service.
/// </summary>
public sealed class PaymentService : IPaymentService
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IPaymentProvider _provider;
    private readonly IDateTimeProvider _clock;
    private readonly IOutboxWriter _outbox;
    private readonly PaymentOptions _options;
    private readonly ILogger<PaymentService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PaymentService"/> class.
    /// </summary>
    public PaymentService(
        IApplicationDbContext dbContext,
        IPaymentProvider provider,
        IDateTimeProvider clock,
        IOutboxWriter outbox,
        IOptions<PaymentOptions> options,
        ILogger<PaymentService> logger)
    {
        _dbContext = dbContext;
        _provider = provider;
        _clock = clock;
        _outbox = outbox;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Wallet> GetOrCreateWalletAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var wallet = await _dbContext.Wallets.FirstOrDefaultAsync(w => w.UserId == userId, cancellationToken);
        if (wallet is not null)
        {
            return wallet;
        }

        wallet = new Wallet
        {
            UserId = userId,
            Currency = _options.Currency,
            AvailableBalance = 0,
            HeldBalance = 0
        };
        _dbContext.Wallets.Add(wallet);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return wallet;
    }

    /// <inheritdoc />
    public async Task TopUpWalletAsync(
        Guid userId,
        decimal amount,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        if (amount <= 0)
        {
            throw new BusinessRuleException("Top-up amount must be positive.", "Payment.InvalidAmount");
        }

        if (await AlreadyProcessedAsync(idempotencyKey, cancellationToken))
        {
            return;
        }

        var wallet = await GetOrCreateWalletAsync(userId, cancellationToken);
        wallet.AvailableBalance += amount;
        AddWalletTx(wallet, WalletTransactionType.Credit, amount, "WalletTopUp", null, "Wallet top-up", idempotencyKey);
        await _outbox.EnqueueAsync(new WalletUpdatedIntegrationEvent(userId, wallet.Id, wallet.AvailableBalance, wallet.HeldBalance), cancellationToken);
        await WriteAuditAsync("Wallet.TopUp", nameof(Wallet), wallet.Id.ToString("D"), null, new { amount }, userId, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<PaymentResponse> InitiateAppointmentPaymentAsync(
        Guid appointmentId,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        var existing = await FindByIdempotencyAsync(idempotencyKey, cancellationToken);
        if (existing is not null)
        {
            return ToResponse(existing);
        }

        var appointment = await _dbContext.Appointments
            .Include(a => a.Customer)
            .Include(a => a.Payment)
            .FirstOrDefaultAsync(a => a.Id == appointmentId, cancellationToken)
            ?? throw new NotFoundException(nameof(Appointment), appointmentId);

        if (appointment.Payment is { Status: PaymentStatus.Captured or PaymentStatus.Authorized })
        {
            throw new ConflictException("Appointment is already paid.");
        }

        var order = await _provider.CreateOrderAsync(
            appointment.ConsultationFeeSnapshot,
            _options.Currency,
            $"appt_{appointment.Id:N}",
            new Dictionary<string, string> { ["appointmentId"] = appointment.Id.ToString("D") },
            cancellationToken);

        var payment = new Payment
        {
            UserId = appointment.Customer.UserId,
            Amount = appointment.ConsultationFeeSnapshot,
            Currency = _options.Currency,
            Status = PaymentStatus.Pending,
            Purpose = PaymentPurpose.Appointment,
            Provider = ParseProvider(),
            ProviderOrderId = order.OrderId,
            AppointmentId = appointment.Id,
            IdempotencyKey = idempotencyKey
        };
        _dbContext.Payments.Add(payment);
        appointment.PaymentStatus = PaymentStatus.Pending;
        await WriteAuditAsync("Payment.Initiate", nameof(Payment), null, null, new { appointmentId, order.OrderId }, appointment.Customer.UserId, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return ToResponse(payment);
    }

    /// <inheritdoc />
    public async Task<PaymentResponse> ConfirmAppointmentPaymentAsync(
        Guid paymentId,
        string providerPaymentId,
        string signature,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        var payment = await _dbContext.Payments
            .Include(p => p.Appointment)
            .FirstOrDefaultAsync(p => p.Id == paymentId, cancellationToken)
            ?? throw new NotFoundException(nameof(Payment), paymentId);

        if (payment.Status == PaymentStatus.Captured)
        {
            return ToResponse(payment);
        }

        if (string.IsNullOrWhiteSpace(payment.ProviderOrderId))
        {
            throw new BusinessRuleException("Payment order is missing.", "Payment.OrderMissing");
        }

        var verification = await _provider.VerifyPaymentSignatureAsync(
            payment.ProviderOrderId,
            providerPaymentId,
            signature,
            cancellationToken);
        if (!verification.IsValid)
        {
            payment.Status = PaymentStatus.Failed;
            payment.FailedAtUtc = _clock.UtcNow;
            payment.FailureReason = "Invalid payment signature.";
            await _dbContext.SaveChangesAsync(cancellationToken);
            throw new BusinessRuleException("Payment signature verification failed.", "Payment.InvalidSignature");
        }

        payment.ProviderPaymentId = providerPaymentId;
        payment.Status = PaymentStatus.Captured;
        payment.CapturedAtUtc = _clock.UtcNow;
        if (payment.Appointment is not null)
        {
            payment.Appointment.PaymentStatus = PaymentStatus.Captured;
        }

        CreateInvoice(payment);
        await _outbox.EnqueueAsync(
            new AppointmentPaidIntegrationEvent(payment.AppointmentId!.Value, payment.Id, payment.Amount),
            cancellationToken);
        await _outbox.EnqueueAsync(
            new PaymentCapturedIntegrationEvent(payment.Id, payment.Amount, payment.Purpose.ToString()),
            cancellationToken);
        await WriteAuditAsync("Payment.Captured", nameof(Payment), payment.Id.ToString("D"), null, new { providerPaymentId }, payment.UserId, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return ToResponse(payment);
    }

    /// <inheritdoc />
    public async Task<PaymentResponse> PlaceEmergencyHoldAsync(
        Guid emergencyRequestId,
        Guid customerUserId,
        decimal amount,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        var existing = await FindByIdempotencyAsync(idempotencyKey, cancellationToken);
        if (existing is not null)
        {
            return ToResponse(existing);
        }

        if (amount <= 0)
        {
            throw new BusinessRuleException("Emergency fee must be positive.", "Payment.InvalidAmount");
        }

        var sos = await _dbContext.EmergencyRequests
            .Include(r => r.Payment)
            .FirstOrDefaultAsync(r => r.Id == emergencyRequestId, cancellationToken)
            ?? throw new NotFoundException(nameof(EmergencyRequest), emergencyRequestId);

        if (sos.Payment is { Status: PaymentStatus.Authorized or PaymentStatus.Captured })
        {
            return ToResponse(sos.Payment);
        }

        var wallet = await GetOrCreateWalletAsync(customerUserId, cancellationToken);
        if (wallet.AvailableBalance >= amount)
        {
            wallet.AvailableBalance -= amount;
            wallet.HeldBalance += amount;
            AddWalletTx(wallet, WalletTransactionType.Hold, amount, nameof(EmergencyRequest), sos.Id, "Emergency hold", idempotencyKey);

            var payment = new Payment
            {
                UserId = customerUserId,
                Amount = amount,
                Currency = _options.Currency,
                Status = PaymentStatus.Authorized,
                Purpose = PaymentPurpose.EmergencyHold,
                Provider = PaymentProviderType.Wallet,
                ProviderPaymentId = $"wallet_hold_{Guid.NewGuid():N}",
                EmergencyRequestId = sos.Id,
                IdempotencyKey = idempotencyKey,
                AuthorizedAtUtc = _clock.UtcNow
            };
            _dbContext.Payments.Add(payment);
            sos.EmergencyFeeSnapshot = amount;
            await _outbox.EnqueueAsync(new EmergencyHoldCreatedIntegrationEvent(sos.Id, payment.Id, amount), cancellationToken);
            await _outbox.EnqueueAsync(
                new WalletUpdatedIntegrationEvent(customerUserId, wallet.Id, wallet.AvailableBalance, wallet.HeldBalance),
                cancellationToken);
            await WriteAuditAsync("Emergency.Hold", nameof(Payment), null, null, new { sos.Id, amount }, customerUserId, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return ToResponse(payment);
        }

        var hold = await _provider.AuthorizeHoldAsync(
            amount,
            _options.Currency,
            $"sos_{sos.Id:N}",
            new Dictionary<string, string> { ["emergencyRequestId"] = sos.Id.ToString("D") },
            cancellationToken);

        var gatewayPayment = new Payment
        {
            UserId = customerUserId,
            Amount = amount,
            Currency = _options.Currency,
            Status = PaymentStatus.Authorized,
            Purpose = PaymentPurpose.EmergencyHold,
            Provider = ParseProvider(),
            ProviderOrderId = hold.OrderId,
            ProviderPaymentId = hold.PaymentId,
            EmergencyRequestId = sos.Id,
            IdempotencyKey = idempotencyKey,
            AuthorizedAtUtc = _clock.UtcNow
        };
        _dbContext.Payments.Add(gatewayPayment);
        sos.EmergencyFeeSnapshot = amount;
        await _outbox.EnqueueAsync(new EmergencyHoldCreatedIntegrationEvent(sos.Id, gatewayPayment.Id, amount), cancellationToken);
        await WriteAuditAsync("Emergency.Hold.Gateway", nameof(Payment), null, null, new { sos.Id, amount }, customerUserId, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return ToResponse(gatewayPayment);
    }

    /// <inheritdoc />
    public async Task<PaymentResponse> CaptureEmergencyHoldAsync(
        Guid emergencyRequestId,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        var payment = await _dbContext.Payments
            .FirstOrDefaultAsync(p => p.EmergencyRequestId == emergencyRequestId, cancellationToken)
            ?? throw new NotFoundException(nameof(Payment), emergencyRequestId);

        if (payment.Status == PaymentStatus.Captured)
        {
            return ToResponse(payment);
        }

        if (payment.Status != PaymentStatus.Authorized)
        {
            throw new ConflictException("Emergency payment is not in an authorized hold state.");
        }

        if (payment.Provider == PaymentProviderType.Wallet)
        {
            var wallet = await GetOrCreateWalletAsync(payment.UserId, cancellationToken);
            if (wallet.HeldBalance < payment.Amount)
            {
                throw new BusinessRuleException("Insufficient held balance.", "Payment.InsufficientHold");
            }

            wallet.HeldBalance -= payment.Amount;
            AddWalletTx(wallet, WalletTransactionType.Capture, payment.Amount, nameof(EmergencyRequest), emergencyRequestId, "Emergency capture", idempotencyKey);
            await _outbox.EnqueueAsync(
                new WalletUpdatedIntegrationEvent(payment.UserId, wallet.Id, wallet.AvailableBalance, wallet.HeldBalance),
                cancellationToken);
        }
        else
        {
            await _provider.CaptureAsync(payment.ProviderPaymentId!, payment.Amount, cancellationToken);
        }

        payment.Status = PaymentStatus.Captured;
        payment.CapturedAtUtc = _clock.UtcNow;
        CreateInvoice(payment);
        await _outbox.EnqueueAsync(
            new PaymentCapturedIntegrationEvent(payment.Id, payment.Amount, payment.Purpose.ToString()),
            cancellationToken);
        await WriteAuditAsync("Emergency.Capture", nameof(Payment), payment.Id.ToString("D"), null, new { payment.Amount }, payment.UserId, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return ToResponse(payment);
    }

    /// <inheritdoc />
    public async Task ReleaseEmergencyHoldAsync(
        Guid emergencyRequestId,
        string reason,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        var payment = await _dbContext.Payments
            .FirstOrDefaultAsync(p => p.EmergencyRequestId == emergencyRequestId, cancellationToken);
        if (payment is null || payment.Status is PaymentStatus.Cancelled or PaymentStatus.Refunded or PaymentStatus.Captured)
        {
            return;
        }

        if (payment.Provider == PaymentProviderType.Wallet)
        {
            var wallet = await GetOrCreateWalletAsync(payment.UserId, cancellationToken);
            var releaseAmount = Math.Min(wallet.HeldBalance, payment.Amount);
            wallet.HeldBalance -= releaseAmount;
            wallet.AvailableBalance += releaseAmount;
            AddWalletTx(wallet, WalletTransactionType.Release, releaseAmount, nameof(EmergencyRequest), emergencyRequestId, reason, idempotencyKey);
            await _outbox.EnqueueAsync(
                new WalletUpdatedIntegrationEvent(payment.UserId, wallet.Id, wallet.AvailableBalance, wallet.HeldBalance),
                cancellationToken);
        }
        else if (!string.IsNullOrWhiteSpace(payment.ProviderPaymentId))
        {
            await _provider.ReleaseHoldAsync(payment.ProviderPaymentId, cancellationToken);
        }

        payment.Status = PaymentStatus.Cancelled;
        payment.FailureReason = reason;
        await WriteAuditAsync("Emergency.Release", nameof(Payment), payment.Id.ToString("D"), null, new { reason }, payment.UserId, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<RefundResponse> RefundPaymentAsync(
        Guid paymentId,
        decimal? amount,
        string? reason,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        var existingRefund = await _dbContext.Refunds
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.IdempotencyKey == idempotencyKey, cancellationToken);
        if (existingRefund is not null)
        {
            return new RefundResponse(existingRefund.Id, existingRefund.PaymentId, existingRefund.Amount, existingRefund.Status.ToString(), existingRefund.ProviderRefundId);
        }

        var payment = await _dbContext.Payments
            .Include(p => p.Refunds)
            .FirstOrDefaultAsync(p => p.Id == paymentId, cancellationToken)
            ?? throw new NotFoundException(nameof(Payment), paymentId);

        if (payment.Status is not (PaymentStatus.Captured or PaymentStatus.PartiallyRefunded))
        {
            throw new ConflictException("Only captured payments can be refunded.");
        }

        var alreadyRefunded = payment.Refunds.Where(r => r.Status == RefundStatus.Succeeded).Sum(r => r.Amount);
        var refundAmount = amount ?? (payment.Amount - alreadyRefunded);
        if (refundAmount <= 0 || refundAmount > payment.Amount - alreadyRefunded)
        {
            throw new BusinessRuleException("Invalid refund amount.", "Payment.InvalidRefundAmount");
        }

        string? providerRefundId = null;
        if (payment.Provider == PaymentProviderType.Wallet)
        {
            var wallet = await GetOrCreateWalletAsync(payment.UserId, cancellationToken);
            wallet.AvailableBalance += refundAmount;
            AddWalletTx(wallet, WalletTransactionType.Refund, refundAmount, nameof(Payment), payment.Id, reason ?? "Refund", idempotencyKey);
            await _outbox.EnqueueAsync(
                new WalletUpdatedIntegrationEvent(payment.UserId, wallet.Id, wallet.AvailableBalance, wallet.HeldBalance),
                cancellationToken);
            providerRefundId = $"wallet_rfnd_{Guid.NewGuid():N}";
        }
        else
        {
            var result = await _provider.RefundAsync(payment.ProviderPaymentId!, refundAmount, reason, cancellationToken);
            providerRefundId = result.RefundId;
        }

        var refund = new Refund
        {
            PaymentId = payment.Id,
            Amount = refundAmount,
            Status = RefundStatus.Succeeded,
            ProviderRefundId = providerRefundId,
            Reason = reason,
            IdempotencyKey = idempotencyKey,
            CompletedAtUtc = _clock.UtcNow
        };
        _dbContext.Refunds.Add(refund);

        payment.Status = alreadyRefunded + refundAmount >= payment.Amount
            ? PaymentStatus.Refunded
            : PaymentStatus.PartiallyRefunded;

        if (payment.AppointmentId.HasValue)
        {
            var appointment = await _dbContext.Appointments.FirstOrDefaultAsync(a => a.Id == payment.AppointmentId, cancellationToken);
            if (appointment is not null && payment.Status == PaymentStatus.Refunded)
            {
                appointment.PaymentStatus = PaymentStatus.Refunded;
            }
        }

        await _outbox.EnqueueAsync(new RefundIssuedIntegrationEvent(payment.Id, refund.Id, refundAmount), cancellationToken);
        await WriteAuditAsync("Payment.Refund", nameof(Refund), null, null, new { paymentId, refundAmount, reason }, payment.UserId, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return new RefundResponse(refund.Id, payment.Id, refund.Amount, refund.Status.ToString(), refund.ProviderRefundId);
    }

    /// <inheritdoc />
    public async Task ProcessWebhookAsync(
        string provider,
        string eventId,
        string eventType,
        string rawBody,
        string signature,
        CancellationToken cancellationToken = default)
    {
        if (!_provider.VerifyWebhookSignature(rawBody, signature))
        {
            throw new BusinessRuleException("Invalid webhook signature.", "Payment.InvalidWebhookSignature");
        }

        var duplicate = await _dbContext.ProcessedWebhooks
            .AnyAsync(w => w.Provider == provider && w.EventId == eventId, cancellationToken);
        if (duplicate)
        {
            _logger.LogInformation("Ignoring duplicate webhook {Provider}/{EventId}", provider, eventId);
            return;
        }

        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawBody))).ToLowerInvariant();
        _dbContext.ProcessedWebhooks.Add(new ProcessedWebhook
        {
            Provider = provider,
            EventId = eventId,
            EventType = eventType,
            PayloadHash = hash,
            ProcessedAtUtc = _clock.UtcNow
        });

        using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(rawBody) ? "{}" : rawBody);
        if (eventType.Contains("captured", StringComparison.OrdinalIgnoreCase)
            && doc.RootElement.TryGetProperty("payload", out var payload)
            && payload.TryGetProperty("payment", out var paymentNode)
            && paymentNode.TryGetProperty("entity", out var entity)
            && entity.TryGetProperty("order_id", out var orderIdNode))
        {
            var orderId = orderIdNode.GetString();
            var paymentId = entity.TryGetProperty("id", out var idNode) ? idNode.GetString() : null;
            var payment = await _dbContext.Payments
                .Include(p => p.Appointment)
                .FirstOrDefaultAsync(p => p.ProviderOrderId == orderId, cancellationToken);
            if (payment is not null && payment.Status != PaymentStatus.Captured)
            {
                payment.ProviderPaymentId = paymentId;
                payment.Status = PaymentStatus.Captured;
                payment.CapturedAtUtc = _clock.UtcNow;
                if (payment.Appointment is not null)
                {
                    payment.Appointment.PaymentStatus = PaymentStatus.Captured;
                }

                CreateInvoice(payment);
                if (payment.AppointmentId.HasValue)
                {
                    await _outbox.EnqueueAsync(
                        new AppointmentPaidIntegrationEvent(payment.AppointmentId.Value, payment.Id, payment.Amount),
                        cancellationToken);
                }

                await _outbox.EnqueueAsync(
                    new PaymentCapturedIntegrationEvent(payment.Id, payment.Amount, payment.Purpose.ToString()),
                    cancellationToken);
            }
        }

        await WriteAuditAsync("Webhook.Processed", nameof(ProcessedWebhook), eventId, null, new { provider, eventType }, null, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task WriteAuditAsync(
        string action,
        string entityName,
        string? entityId,
        object? oldValues,
        object? newValues,
        Guid? userId,
        CancellationToken cancellationToken = default)
    {
        _dbContext.AuditLogs.Add(new AuditLog
        {
            UserId = userId,
            Action = action,
            EntityName = entityName,
            EntityId = entityId,
            OldValuesJson = oldValues is null ? null : JsonSerializer.Serialize(oldValues),
            NewValuesJson = newValues is null ? null : JsonSerializer.Serialize(newValues),
            CorrelationId = Guid.NewGuid().ToString("N")
        });
        await Task.CompletedTask;
    }

    private void CreateInvoice(Payment payment)
    {
        if (payment.Invoice is not null)
        {
            return;
        }

        var invoice = new Invoice
        {
            InvoiceNumber = $"INV-{_clock.UtcNow:yyyyMMdd}-{payment.Id.ToString("N")[..8].ToUpperInvariant()}",
            UserId = payment.UserId,
            PaymentId = payment.Id,
            Amount = payment.Amount,
            TaxAmount = 0,
            TotalAmount = payment.Amount,
            Currency = payment.Currency,
            IssuedAtUtc = _clock.UtcNow,
            LineItemsJson = JsonSerializer.Serialize(new[]
            {
                new { payment.Purpose, payment.Amount }
            })
        };
        payment.Invoice = invoice;
        _dbContext.Invoices.Add(invoice);
    }

    private void AddWalletTx(
        Wallet wallet,
        WalletTransactionType type,
        decimal amount,
        string? referenceType,
        Guid? referenceId,
        string description,
        string? idempotencyKey)
    {
        _dbContext.WalletTransactions.Add(new WalletTransaction
        {
            WalletId = wallet.Id,
            Type = type,
            Amount = amount,
            AvailableBalanceAfter = wallet.AvailableBalance,
            HeldBalanceAfter = wallet.HeldBalance,
            ReferenceType = referenceType,
            ReferenceId = referenceId,
            Description = description,
            IdempotencyKey = idempotencyKey
        });
    }

    private async Task<bool> AlreadyProcessedAsync(string idempotencyKey, CancellationToken cancellationToken) =>
        await _dbContext.WalletTransactions.AnyAsync(t => t.IdempotencyKey == idempotencyKey, cancellationToken)
        || await _dbContext.Payments.AnyAsync(p => p.IdempotencyKey == idempotencyKey, cancellationToken);

    private async Task<Payment?> FindByIdempotencyAsync(string key, CancellationToken cancellationToken) =>
        await _dbContext.Payments.FirstOrDefaultAsync(p => p.IdempotencyKey == key, cancellationToken);

    private PaymentProviderType ParseProvider() =>
        _options.Provider.Equals("Razorpay", StringComparison.OrdinalIgnoreCase)
            ? PaymentProviderType.Razorpay
            : PaymentProviderType.Fake;

    private static PaymentResponse ToResponse(Payment payment) =>
        new(
            payment.Id,
            payment.Amount,
            payment.Currency,
            payment.Status.ToString(),
            payment.Purpose.ToString(),
            payment.Provider.ToString(),
            payment.ProviderOrderId,
            payment.ProviderPaymentId,
            payment.AppointmentId,
            payment.EmergencyRequestId,
            payment.AuthorizedAtUtc,
            payment.CapturedAtUtc);
}
