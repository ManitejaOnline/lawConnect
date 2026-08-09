using LawConnect.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace LawConnect.Infrastructure.Sms;

/// <summary>
/// SMS transport that delivers messages through structured application logs.
/// Suitable as the default channel until an external SMS provider is configured.
/// </summary>
public sealed class LoggingSmsService : ISmsService
{
    private readonly ILogger<LoggingSmsService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="LoggingSmsService"/> class.
    /// </summary>
    public LoggingSmsService(ILogger<LoggingSmsService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public Task SendAsync(
        string phoneNumber,
        string message,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(phoneNumber);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        _logger.LogInformation(
            "SMS dispatched to {PhoneNumber}: {Message}",
            phoneNumber,
            message);

        return Task.CompletedTask;
    }
}
