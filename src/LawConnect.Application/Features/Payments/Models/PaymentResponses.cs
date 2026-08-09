#pragma warning disable CS1591
namespace LawConnect.Application.Features.Payments.Models;

/// <summary>Payment DTO.</summary>
public sealed record PaymentResponse(
    Guid PaymentId,
    decimal Amount,
    string Currency,
    string Status,
    string Purpose,
    string Provider,
    string? ProviderOrderId,
    string? ProviderPaymentId,
    Guid? AppointmentId,
    Guid? EmergencyRequestId,
    DateTime? AuthorizedAtUtc,
    DateTime? CapturedAtUtc);

/// <summary>Refund DTO.</summary>
public sealed record RefundResponse(
    Guid RefundId,
    Guid PaymentId,
    decimal Amount,
    string Status,
    string? ProviderRefundId);

/// <summary>Wallet balance DTO.</summary>
public sealed record WalletResponse(
    Guid WalletId,
    decimal AvailableBalance,
    decimal HeldBalance,
    string Currency);

/// <summary>Wallet transaction DTO.</summary>
public sealed record WalletTransactionResponse(
    Guid TransactionId,
    string Type,
    decimal Amount,
    decimal AvailableBalanceAfter,
    decimal HeldBalanceAfter,
    string Description,
    DateTime CreatedAtUtc,
    string? ReferenceType,
    Guid? ReferenceId);

/// <summary>Invoice DTO.</summary>
public sealed record InvoiceResponse(
    Guid InvoiceId,
    string InvoiceNumber,
    Guid PaymentId,
    decimal Amount,
    decimal TaxAmount,
    decimal TotalAmount,
    string Currency,
    DateTime IssuedAtUtc);
