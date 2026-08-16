using LawConnect.Domain.Common;
using LawConnect.Domain.Enums;

namespace LawConnect.Domain.Entities;

/// <summary>
/// A discrete step in a <see cref="Case"/>'s progress (e.g. "Case Filed", "Evidence Submitted").
/// </summary>
public sealed class CaseMilestone : BaseEntity
{
    public Guid CaseId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public MilestoneStatus Status { get; set; } = MilestoneStatus.Pending;
    public DateTime? DueDate { get; set; }
    public DateTime? CompletedDate { get; set; }

    public Case Case { get; set; } = null!;
    public ICollection<CaseDocument> Documents { get; set; } = new List<CaseDocument>();
}
