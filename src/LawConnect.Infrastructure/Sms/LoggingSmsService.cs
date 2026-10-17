using LawConnect.Application.Common.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LawConnect.Infrastructure.Sms;

/// <summary>
/// SMS transport that delivers messages through structured application logs.
/// Suitable as the default channel until an external SMS provider is configured.
/// </summary>
public sealed class LoggingSmsService : ISmsService
{
    private readonly ILogger<LoggingSmsService> _logger;
    private readonly bool _logMessageBody;

    /// <summary>
    /// Initializes a new instance of the <see cref="LoggingSmsService"/> class.
    /// </summary>
    public LoggingSmsService(ILogger<LoggingSmsService> logger, IHostEnvironment environment)
    {
        _logger = logger;
        // SMS bodies contain one-time login codes; they must never reach shared (production) logs.
        _logMessageBody = environment.IsDevelopment()
            || environment.IsEnvironment("Testing")
            || environment.IsEnvironment("Docker");
    }

    /// <inheritdoc />
    public Task SendAsync(
        string phoneNumber,
        string message,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(phoneNumber);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        if (_logMessageBody)
        {
            _logger.LogInformation(
                "SMS dispatched to {PhoneNumber}: {Message}",
                phoneNumber,
                message);
        }
        else
        {
            _logger.LogInformation(
                "SMS dispatched to {PhoneNumber} (message body redacted)",
                MaskPhone(phoneNumber));
        }

        return Task.CompletedTask;
    }

    private static string MaskPhone(string phoneNumber) =>
        phoneNumber.Length <= 4
            ? "****"
            : string.Concat(new string('*', phoneNumber.Length - 4), phoneNumber[^4..]);
}
