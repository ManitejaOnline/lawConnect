namespace LawConnect.Application.Common.Interfaces;

/// <summary>
/// Geospatial and mapping operations.
/// </summary>
public interface IMapService
{
    /// <summary>
    /// Resolves an address to geographic coordinates.
    /// </summary>
    /// <param name="address">Free-form address.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Coordinates when found; otherwise <c>null</c>.</returns>
    Task<GeoCoordinates?> GeocodeAsync(
        string address,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates distance in kilometers between two coordinates.
    /// </summary>
    /// <param name="from">Origin coordinates.</param>
    /// <param name="to">Destination coordinates.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Distance in kilometers.</returns>
    Task<double> GetDistanceKmAsync(
        GeoCoordinates from,
        GeoCoordinates to,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Geographic coordinates.
/// </summary>
/// <param name="Latitude">Latitude in decimal degrees.</param>
/// <param name="Longitude">Longitude in decimal degrees.</param>
public sealed record GeoCoordinates(double Latitude, double Longitude);
