namespace LawConnect.Application.Common.Interfaces;

/// <summary>
/// Sends transactional email messages.
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Sends an HTML email message.
    /// </summary>
    /// <param name="toEmail">Recipient address.</param>
    /// <param name="subject">Subject line.</param>
    /// <param name="htmlBody">HTML body.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendAsync(
        string toEmail,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken = default);
}
