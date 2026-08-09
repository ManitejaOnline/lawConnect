#pragma warning disable CS1591
using LawConnect.Application.Common.Events;
using LawConnect.Domain.Enums;

namespace LawConnect.Application.Features.Payments.Events;

/// <summary>Raised when an appointment payment is captured.</summary>
public sealed class AppointmentPaidIntegrationEvent : IntegrationEventBase
{
    public AppointmentPaidIntegrationEvent(Guid appointmentId, Guid paymentId, decimal amount)
    {
        AppointmentId = appointmentId;
        PaymentId = paymentId;
        Amount = amount;
    }

    public Guid AppointmentId { get; }
    public Guid PaymentId { get; }
    public decimal Amount { get; }
    public override string EventType => "payments.appointment_paid";
}

/// <summary>Raised when an emergency hold is placed.</summary>
public sealed class EmergencyHoldCreatedIntegrationEvent : IntegrationEventBase
{
    public EmergencyHoldCreatedIntegrationEvent(Guid emergencyRequestId, Guid paymentId, decimal amount)
    {
        EmergencyRequestId = emergencyRequestId;
        PaymentId = paymentId;
        Amount = amount;
    }

    public Guid EmergencyRequestId { get; }
    public Guid PaymentId { get; }
    public decimal Amount { get; }
    public override string EventType => "payments.emergency_hold";
}

/// <summary>Raised when a payment is captured.</summary>
public sealed class PaymentCapturedIntegrationEvent : IntegrationEventBase
{
    public PaymentCapturedIntegrationEvent(Guid paymentId, decimal amount, string purpose)
    {
        PaymentId = paymentId;
        Amount = amount;
        Purpose = purpose;
    }

    public Guid PaymentId { get; }
    public decimal Amount { get; }
    public string Purpose { get; }
    public override string EventType => "payments.payment_captured";
}

/// <summary>Raised when a refund is issued.</summary>
public sealed class RefundIssuedIntegrationEvent : IntegrationEventBase
{
    public RefundIssuedIntegrationEvent(Guid paymentId, Guid refundId, decimal amount)
    {
        PaymentId = paymentId;
        RefundId = refundId;
        Amount = amount;
    }

    public Guid PaymentId { get; }
    public Guid RefundId { get; }
    public decimal Amount { get; }
    public override string EventType => "payments.refund_issued";
}

/// <summary>Raised when wallet balances change.</summary>
public sealed class WalletUpdatedIntegrationEvent : IntegrationEventBase
{
    public WalletUpdatedIntegrationEvent(Guid userId, Guid walletId, decimal availableBalance, decimal heldBalance)
    {
        UserId = userId;
        WalletId = walletId;
        AvailableBalance = availableBalance;
        HeldBalance = heldBalance;
    }

    public Guid UserId { get; }
    public Guid WalletId { get; }
    public decimal AvailableBalance { get; }
    public decimal HeldBalance { get; }
    public override string EventType => "payments.wallet_updated";
}
