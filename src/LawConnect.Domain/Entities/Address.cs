using LawConnect.Domain.Common;

namespace LawConnect.Domain.Entities;

public sealed class Address : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid? CityId { get; set; }
    public string Line1 { get; set; } = string.Empty;
    public string? Line2 { get; set; }
    public string? PostalCode { get; set; }
    public string? Landmark { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public bool IsPrimary { get; set; }
    public string? Label { get; set; }

    public User User { get; set; } = null!;
    public City? City { get; set; }
}
