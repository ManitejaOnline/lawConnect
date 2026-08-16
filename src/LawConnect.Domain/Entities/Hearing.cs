using LawConnect.Domain.Common;
using LawConnect.Domain.Enums;

namespace LawConnect.Domain.Entities;

/// <summary>
/// A scheduled or completed court appearance tied to a <see cref="Case"/>.
/// </summary>
public sealed class Hearing : BaseEntity
{
    public Guid CaseId { get; set; }
    public DateTime HearingDate { get; set; }
    public string? CourtName { get; set; }
    public string? HearingType { get; set; }
    public string? Purpose { get; set; }
    public HearingStatus Status { get; set; } = HearingStatus.Scheduled;

    /// <summary>Recorded once the hearing is completed.</summary>
    public string? Outcome { get; set; }

    /// <summary>Recorded once the hearing is completed.</summary>
    public string? Notes { get; set; }

    /// <summary>What the lawyer should do next, recorded once the hearing is completed. A genuinely new hearing date is its own new <see cref="Hearing"/> row.</summary>
    public string? NextAction { get; set; }

    public Case Case { get; set; } = null!;
    public ICollection<CaseDocument> Documents { get; set; } = new List<CaseDocument>();
}
