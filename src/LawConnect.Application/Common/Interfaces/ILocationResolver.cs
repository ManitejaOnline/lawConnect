using LawConnect.Application.Common.Geo;

namespace LawConnect.Application.Common.Interfaces;

/// <summary>
/// Resolves GPS coordinates to structured Country/State/City.
/// Customer GPS is authoritative — clients must not supply city/state strings.
/// </summary>
public interface ILocationResolver
{
    /// <summary>Reverse-geocodes coordinates and upserts directory rows.</summary>
    Task<BoundGeoPlace?> ResolveFromCoordinatesAsync(
        double latitude,
        double longitude,
        CancellationToken cancellationToken = default);
}

/// <summary>External reverse-geocode provider (Nominatim, etc.).</summary>
public interface IReverseGeocoder
{
    /// <summary>Returns place names for coordinates, or null when unknown.</summary>
    Task<ResolvedGeoPlace?> ReverseAsync(
        double latitude,
        double longitude,
        CancellationToken cancellationToken = default);
}
