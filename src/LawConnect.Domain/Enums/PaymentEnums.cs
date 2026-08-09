#pragma warning disable CS1591
namespace LawConnect.Domain.Enums;

/// <summary>
/// Payment lifecycle status.
/// </summary>
public enum PaymentStatus
{
    Pending = 0,
    Authorized = 1,
    Captured = 2,
    Failed = 3,
    Cancelled = 4,
    Refunded = 5,
    PartiallyRefunded = 6
}

/// <summary>
/// Business purpose of a payment.
/// </summary>
public enum PaymentPurpose
{
    Appointment = 0,
    EmergencyHold = 1
}

/// <summary>
/// Wallet ledger entry type.
/// </summary>
public enum WalletTransactionType
{
    Credit = 0,
    Debit = 1,
    Hold = 2,
    Capture = 3,
    Release = 4,
    Refund = 5
}

/// <summary>
/// Refund processing status.
/// </summary>
public enum RefundStatus
{
    Pending = 0,
    Succeeded = 1,
    Failed = 2
}

/// <summary>
/// Supported payment providers.
/// </summary>
public enum PaymentProviderType
{
    Razorpay = 0,
    Stripe = 1,
    Wallet = 2,
    Fake = 3
}

/// <summary>
/// Outbox delivery status.
/// </summary>
public enum OutboxMessageStatus
{
    Pending = 0,
    Processing = 1,
    Published = 2,
    Failed = 3
}
