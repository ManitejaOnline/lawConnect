using LawConnect.Domain.Common;
using LawConnect.Domain.Enums;

namespace LawConnect.Domain.Entities;

/// <summary>
/// Per-recipient delivery and read receipt for a message.
/// </summary>
public sealed class MessageReadStatus : BaseEntity
{
    public Guid MessageId { get; set; }
    public Guid UserId { get; set; }
    public MessageDeliveryState State { get; set; } = MessageDeliveryState.Sent;
    public DateTime? DeliveredAtUtc { get; set; }
    public DateTime? ReadAtUtc { get; set; }

    public Message Message { get; set; } = null!;
    public User User { get; set; } = null!;
}
