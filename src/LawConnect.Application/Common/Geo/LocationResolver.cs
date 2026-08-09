using LawConnect.Application.Common.Interfaces;
using LawConnect.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace LawConnect.Application.Common.Geo;

/// <summary>
/// Reverse-geocodes GPS, caches results, and binds names to Country/State/City rows.
/// Falls back to nearest directory city when the provider is unavailable.
/// </summary>
public sealed class LocationResolver : ILocationResolver
{
    private const double NearestCityMaxKm = 150d;
    private readonly IApplicationDbContext _dbContext;
    private readonly IReverseGeocoder _geocoder;
    private readonly IMemoryCache _cache;

    /// <summary>Initializes a new instance of the <see cref="LocationResolver"/> class.</summary>
    public LocationResolver(
        IApplicationDbContext dbContext,
        IReverseGeocoder geocoder,
        IMemoryCache cache)
    {
        _dbContext = dbContext;
        _geocoder = geocoder;
        _cache = cache;
    }

    /// <inheritdoc />
    public async Task<BoundGeoPlace?> ResolveFromCoordinatesAsync(
        double latitude,
        double longitude,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = CacheKey(latitude, longitude);
        if (_cache.TryGetValue(cacheKey, out BoundGeoPlace? cached) && cached is not null)
        {
            return cached;
        }

        var raw = await _geocoder.ReverseAsync(latitude, longitude, cancellationToken);
        BoundGeoPlace? bound = null;
        if (raw is not null)
        {
            bound = await BindAsync(raw, latitude, longitude, cancellationToken);
        }

        bound ??= await NearestCityAsync(latitude, longitude, cancellationToken);
        if (bound is not null)
        {
            _cache.Set(cacheKey, bound, TimeSpan.FromHours(12));
        }

        return bound;
    }

    private async Task<BoundGeoPlace> BindAsync(
        ResolvedGeoPlace place,
        double latitude,
        double longitude,
        CancellationToken cancellationToken)
    {
        var iso2 = place.CountryIso2.Trim().ToUpperInvariant();
        var iso3 = string.IsNullOrWhiteSpace(place.CountryIso3)
            ? iso2.PadRight(3, 'X')[..3]
            : place.CountryIso3.Trim().ToUpperInvariant();
        var countryName = place.CountryName.Trim();
        var stateName = place.StateName.Trim();
        var cityName = place.CityName.Trim();

        var country = await _dbContext.Countries
            .FirstOrDefaultAsync(c => c.Iso2Code == iso2, cancellationToken);
        if (country is null)
        {
            country = new Country
            {
                Name = countryName,
                Iso2Code = iso2,
                Iso3Code = iso3,
                IsActive = true
            };
            _dbContext.Countries.Add(country);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        var state = await _dbContext.States
            .FirstOrDefaultAsync(s => s.CountryId == country.Id && s.Name == stateName, cancellationToken);
        if (state is null)
        {
            state = new State
            {
                CountryId = country.Id,
                Name = stateName,
                Code = string.IsNullOrWhiteSpace(place.StateCode) ? null : place.StateCode.Trim(),
                IsActive = true
            };
            _dbContext.States.Add(state);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        var city = await _dbContext.Cities
            .FirstOrDefaultAsync(c => c.StateId == state.Id && c.Name == cityName, cancellationToken);
        if (city is null)
        {
            city = new City
            {
                StateId = state.Id,
                Name = cityName,
                Latitude = latitude,
                Longitude = longitude,
                IsActive = true
            };
            _dbContext.Cities.Add(city);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return new BoundGeoPlace(
            country.Id,
            state.Id,
            city.Id,
            country.Name,
            state.Name,
            city.Name,
            country.Iso2Code);
    }

    private async Task<BoundGeoPlace?> NearestCityAsync(
        double latitude,
        double longitude,
        CancellationToken cancellationToken)
    {
        var cities = await _dbContext.Cities
            .AsNoTracking()
            .Where(c => c.IsActive && c.Latitude != null && c.Longitude != null)
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.Latitude,
                c.Longitude,
                StateId = c.StateId,
                StateName = c.State.Name,
                CountryId = c.State.CountryId,
                CountryName = c.State.Country.Name,
                CountryIso2 = c.State.Country.Iso2Code
            })
            .ToListAsync(cancellationToken);

        var nearest = cities
            .Select(c => new
            {
                Place = c,
                Distance = GeoDistance.Kilometers(latitude, longitude, c.Latitude!.Value, c.Longitude!.Value)
            })
            .OrderBy(x => x.Distance)
            .FirstOrDefault();

        if (nearest is null || nearest.Distance > NearestCityMaxKm)
        {
            return null;
        }

        return new BoundGeoPlace(
            nearest.Place.CountryId,
            nearest.Place.StateId,
            nearest.Place.Id,
            nearest.Place.CountryName,
            nearest.Place.StateName,
            nearest.Place.Name,
            nearest.Place.CountryIso2);
    }

    private static string CacheKey(double latitude, double longitude) =>
        $"geo:{Math.Round(latitude, 4).ToString(System.Globalization.CultureInfo.InvariantCulture)}:{Math.Round(longitude, 4).ToString(System.Globalization.CultureInfo.InvariantCulture)}";
}
