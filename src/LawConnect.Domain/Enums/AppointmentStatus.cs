namespace LawConnect.Domain.Enums;

/// <summary>
/// Lifecycle status for an appointment booking.
/// </summary>
public enum AppointmentStatus
{
    /// <summary>Booked and awaiting the scheduled time.</summary>
    Scheduled = 0,

    /// <summary>Cancelled by a participant.</summary>
    Cancelled = 1,

    /// <summary>Completed after the scheduled end time.</summary>
    Completed = 2,

    /// <summary>The lawyer did not attend; reported by the customer after the scheduled end time.</summary>
    NoShow = 3
}
