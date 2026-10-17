namespace LawConnect.Domain.Enums;

/// <summary>Lifecycle of a phone-to-phone document transfer. File bytes are never stored.</summary>
public enum DocumentTransferStatus
{
    /// <summary>Lawyer asked; waiting for the customer to approve.</summary>
    Pending = 0,

    /// <summary>Customer approved and holds a single-use upload token.</summary>
    Approved = 1,

    /// <summary>Both devices are connected and bytes are being relayed.</summary>
    Transferring = 2,

    /// <summary>The lawyer received the full file.</summary>
    Completed = 3,

    /// <summary>The customer rejected the request.</summary>
    Rejected = 4,

    /// <summary>The request or approval window elapsed.</summary>
    Expired = 5,

    /// <summary>A device disconnected or the byte count did not match.</summary>
    Interrupted = 6,

    /// <summary>The customer cancelled before the transfer finished.</summary>
    Cancelled = 7
}
