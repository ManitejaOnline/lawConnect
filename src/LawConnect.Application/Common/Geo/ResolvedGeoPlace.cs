namespace LawConnect.Application.Common.Geo;

/// <summary>Raw reverse-geocode result before binding to the geo directory.</summary>
public sealed record ResolvedGeoPlace(
    string CountryName,
    string CountryIso2,
    string CountryIso3,
    string StateName,
    string? StateCode,
    string CityName);

/// <summary>Customer or lawyer location bound to Country/State/City rows.</summary>
public sealed record BoundGeoPlace(
    Guid CountryId,
    Guid StateId,
    Guid CityId,
    string CountryName,
    string StateName,
    string CityName,
    string CountryIso2);
