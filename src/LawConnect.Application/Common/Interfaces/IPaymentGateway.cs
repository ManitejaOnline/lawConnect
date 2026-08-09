namespace LawConnect.Application.Common.Interfaces;

/// <summary>
/// Payment gateway abstraction (Razorpay in production).
/// </summary>
public interface IPaymentGateway
{
    /// <summary>
    /// Creates a payment order.
    /// </summary>
    /// <param name="amount">Order amount.</param>
    /// <param name="currency">ISO currency code.</param>
    /// <param name="receipt">Merchant receipt identifier.</param>
    /// <param name="notes">Optional gateway notes.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Created order details.</returns>
    Task<PaymentOrderResult> CreateOrderAsync(
        decimal amount,
        string currency,
        string receipt,
        IReadOnlyDictionary<string, string>? notes = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies a completed payment signature.
    /// </summary>
    /// <param name="orderId">Gateway order identifier.</param>
    /// <param name="paymentId">Gateway payment identifier.</param>
    /// <param name="signature">Gateway signature.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Verification result.</returns>
    Task<PaymentVerificationResult> VerifyPaymentAsync(
        string orderId,
        string paymentId,
        string signature,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of creating a payment order.
/// </summary>
/// <param name="OrderId">Gateway order identifier.</param>
/// <param name="Amount">Order amount.</param>
/// <param name="Currency">Currency code.</param>
/// <param name="Status">Gateway status.</param>
public sealed record PaymentOrderResult(string OrderId, decimal Amount, string Currency, string Status);

/// <summary>
/// Result of verifying a payment.
/// </summary>
/// <param name="IsValid">Whether the signature is valid.</param>
/// <param name="PaymentId">Gateway payment identifier.</param>
/// <param name="OrderId">Gateway order identifier.</param>
public sealed record PaymentVerificationResult(bool IsValid, string PaymentId, string OrderId);
