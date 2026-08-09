#pragma warning disable CS1591
using LawConnect.Domain.Common;
using LawConnect.Domain.Enums;

namespace LawConnect.Domain.Entities;

/// <summary>
/// Transactional outbox message for reliable integration-event publishing.
/// </summary>
public sealed class OutboxMessage : BaseEntity
{
    public string EventType { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = string.Empty;
    public OutboxMessageStatus Status { get; set; } = OutboxMessageStatus.Pending;
    public int Attempts { get; set; }
    public DateTime? NextAttemptAtUtc { get; set; }
    public string? LastError { get; set; }
    public DateTime? PublishedAtUtc { get; set; }
}
