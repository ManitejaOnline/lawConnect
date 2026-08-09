#pragma warning disable CS1591
using System.Text.Json;
using LawConnect.Application.Common.Events;
using LawConnect.Application.Common.Interfaces;
using LawConnect.Domain.Entities;
using LawConnect.Domain.Enums;
using LawConnect.Shared.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LawConnect.Application.Features.Payments.Services;

/// <summary>
/// Writes integration events into the transactional outbox.
/// </summary>
public interface IOutboxWriter
{
    /// <summary>Enqueues an integration event for reliable publish.</summary>
    Task EnqueueAsync(IIntegrationEvent integrationEvent, CancellationToken cancellationToken = default);
}

/// <summary>
/// Processes pending outbox messages with retry.
/// </summary>
public interface IOutboxProcessor
{
    /// <summary>Publishes due outbox messages.</summary>
    Task ProcessPendingAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Default outbox writer.
/// </summary>
public sealed class OutboxWriter : IOutboxWriter
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IDateTimeProvider _clock;

    /// <summary>
    /// Initializes a new instance of the <see cref="OutboxWriter"/> class.
    /// </summary>
    public OutboxWriter(IApplicationDbContext dbContext, IDateTimeProvider clock)
    {
        _dbContext = dbContext;
        _clock = clock;
    }

    /// <inheritdoc />
    public Task EnqueueAsync(IIntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        _dbContext.OutboxMessages.Add(new OutboxMessage
        {
            EventType = integrationEvent.EventType,
            PayloadJson = JsonSerializer.Serialize(integrationEvent, integrationEvent.GetType()),
            Status = OutboxMessageStatus.Pending,
            NextAttemptAtUtc = _clock.UtcNow,
            Attempts = 0
        });
        return Task.CompletedTask;
    }
}

/// <summary>
/// Publishes outbox messages via <see cref="IIntegrationEventPublisher"/> with retry backoff.
/// </summary>
public sealed class OutboxProcessor : IOutboxProcessor
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IIntegrationEventPublisher _publisher;
    private readonly IDateTimeProvider _clock;
    private readonly PaymentOptions _options;
    private readonly ILogger<OutboxProcessor> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="OutboxProcessor"/> class.
    /// </summary>
    public OutboxProcessor(
        IApplicationDbContext dbContext,
        IIntegrationEventPublisher publisher,
        IDateTimeProvider clock,
        IOptions<PaymentOptions> options,
        ILogger<OutboxProcessor> logger)
    {
        _dbContext = dbContext;
        _publisher = publisher;
        _clock = clock;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task ProcessPendingAsync(CancellationToken cancellationToken = default)
    {
        var now = _clock.UtcNow;
        var batch = await _dbContext.OutboxMessages
            .Where(m =>
                (m.Status == OutboxMessageStatus.Pending || m.Status == OutboxMessageStatus.Failed)
                && (m.NextAttemptAtUtc == null || m.NextAttemptAtUtc <= now)
                && m.Attempts < _options.OutboxMaxAttempts)
            .OrderBy(m => m.CreatedAt)
            .Take(50)
            .ToListAsync(cancellationToken);

        foreach (var message in batch)
        {
            message.Status = OutboxMessageStatus.Processing;
            message.Attempts += 1;
            await _dbContext.SaveChangesAsync(cancellationToken);

            try
            {
                // Publish a lightweight envelope; subscribers can deserialize by EventType.
                await _publisher.PublishAsync(new OutboxEnvelopeIntegrationEvent(message.EventType, message.PayloadJson), cancellationToken);
                message.Status = OutboxMessageStatus.Published;
                message.PublishedAtUtc = _clock.UtcNow;
                message.LastError = null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Outbox publish failed for {MessageId}", message.Id);
                message.Status = message.Attempts >= _options.OutboxMaxAttempts
                    ? OutboxMessageStatus.Failed
                    : OutboxMessageStatus.Pending;
                message.LastError = ex.Message;
                message.NextAttemptAtUtc = _clock.UtcNow.AddSeconds(Math.Pow(2, message.Attempts));
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}

/// <summary>
/// Envelope used when replaying outbox payloads.
/// </summary>
public sealed class OutboxEnvelopeIntegrationEvent : IntegrationEventBase
{
    public OutboxEnvelopeIntegrationEvent(string originalEventType, string payloadJson)
    {
        OriginalEventType = originalEventType;
        PayloadJson = payloadJson;
    }

    public string OriginalEventType { get; }
    public string PayloadJson { get; }
    public override string EventType => "outbox.envelope";
}
