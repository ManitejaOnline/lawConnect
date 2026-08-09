namespace LawConnect.Domain.Enums;

/// <summary>
/// Delivery lifecycle for a message relative to a recipient.
/// </summary>
public enum MessageDeliveryState
{
    Sent = 0,
    Delivered = 1,
    Read = 2
}
