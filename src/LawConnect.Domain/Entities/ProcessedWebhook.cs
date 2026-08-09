#pragma warning disable CS1591
using LawConnect.Domain.Common;

namespace LawConnect.Domain.Entities;

/// <summary>
/// Deduplicates provider webhook deliveries.
/// </summary>
public sealed class ProcessedWebhook : BaseEntity
{
    public string Provider { get; set; } = string.Empty;
    public string EventId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string PayloadHash { get; set; } = string.Empty;
    public DateTime ProcessedAtUtc { get; set; }
}
