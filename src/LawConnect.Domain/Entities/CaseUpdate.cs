using LawConnect.Domain.Common;
using LawConnect.Domain.Enums;

namespace LawConnect.Domain.Entities;

/// <summary>
/// A chronological note logged against a <see cref="Case"/>. Status-change events are represented
/// as auto-generated updates with <see cref="CaseUpdateType.StatusChange"/> so the timeline never
/// needs a separate history table.
/// </summary>
public sealed class CaseUpdate : BaseEntity
{
    public Guid CaseId { get; set; }
    public Guid CreatedByUserId { get; set; }
    public CaseUpdateType UpdateType { get; set; } = CaseUpdateType.General;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public Case Case { get; set; } = null!;
    public ICollection<CaseDocument> Documents { get; set; } = new List<CaseDocument>();
}
