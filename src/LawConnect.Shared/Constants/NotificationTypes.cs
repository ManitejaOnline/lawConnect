namespace LawConnect.Shared.Constants;

/// <summary>
/// Application notification type identifiers.
/// </summary>
public static class NotificationTypes
{
    /// <summary>New chat message push/in-app.</summary>
    public const string NewMessage = "chat.new_message";

    /// <summary>Appointment confirmed.</summary>
    public const string AppointmentConfirmed = "appointment.confirmed";

    /// <summary>Appointment cancelled.</summary>
    public const string AppointmentCancelled = "appointment.cancelled";

    /// <summary>Lawyer accepted a booking.</summary>
    public const string LawyerAcceptedBooking = "appointment.lawyer_accepted";

    /// <summary>Generic notification.</summary>
    public const string General = "general";

    /// <summary>Incoming emergency SOS for a lawyer.</summary>
    public const string EmergencyIncoming = "emergency.incoming";

    /// <summary>SOS assigned / status update for a customer.</summary>
    public const string EmergencyStatus = "emergency.status";

    /// <summary>Another lawyer accepted first.</summary>
    public const string EmergencyTaken = "emergency.taken";
}
