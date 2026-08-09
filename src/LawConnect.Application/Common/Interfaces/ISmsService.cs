namespace LawConnect.Application.Common.Interfaces;

/// <summary>
/// Sends SMS messages (including OTP delivery).
/// </summary>
public interface ISmsService
{
    /// <summary>
    /// Sends an SMS message to a phone number.
    /// </summary>
    /// <param name="phoneNumber">Destination phone number in E.164 format when possible.</param>
    /// <param name="message">Message body.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendAsync(
        string phoneNumber,
        string message,
        CancellationToken cancellationToken = default);
}
