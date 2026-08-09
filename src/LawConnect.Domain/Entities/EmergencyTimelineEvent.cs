using LawConnect.Domain.Common;

namespace LawConnect.Domain.Entities;

/// <summary>
/// Immutable timeline entry for an SOS request.
/// </summary>
public sealed class EmergencyTimelineEvent : BaseEntity
{
    public Guid EmergencyRequestId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public Guid? ActorUserId { get; set; }
    public string? MetadataJson { get; set; }
    public DateTime OccurredAtUtc { get; set; }

    public EmergencyRequest EmergencyRequest { get; set; } = null!;
}
