namespace LawConnect.Domain.Enums;

/// <summary>
/// Category of a <see cref="Entities.CaseUpdate"/> entry.
/// </summary>
public enum CaseUpdateType
{
    /// <summary>General note.</summary>
    General = 0,

    /// <summary>A communication with the client.</summary>
    ClientCommunication = 1,

    /// <summary>A document was filed with the court or a counterparty.</summary>
    DocumentFiled = 2,

    /// <summary>A document was received.</summary>
    DocumentReceived = 3,

    /// <summary>An update related to a hearing.</summary>
    HearingUpdate = 4,

    /// <summary>An update from the court.</summary>
    CourtUpdate = 5,

    /// <summary>A deadline-related note.</summary>
    Deadline = 6,

    /// <summary>Auto-generated when the case's status changes.</summary>
    StatusChange = 7,

    /// <summary>Anything not covered by the other categories.</summary>
    Other = 8
}
