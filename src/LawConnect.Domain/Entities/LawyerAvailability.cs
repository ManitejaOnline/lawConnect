using LawConnect.Domain.Common;
using LawConnect.Domain.Enums;

namespace LawConnect.Domain.Entities;

public sealed class LawyerAvailability : BaseEntity
{
    public Guid LawyerId { get; set; }
    public DayOfWeekFlag DayOfWeek { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public bool IsAvailable { get; set; } = true;
    public string TimeZoneId { get; set; } = "Asia/Kolkata";

    public Lawyer Lawyer { get; set; } = null!;
}
