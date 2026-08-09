using System.Net.Http.Json;
using System.Text.Json.Serialization;
using LawConnect.Application.Common.Geo;
using LawConnect.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace LawConnect.Infrastructure.Geo;

/// <summary>OpenStreetMap Nominatim reverse geocoder.</summary>
public sealed class NominatimReverseGeocoder : IReverseGeocoder
{
    private readonly HttpClient _http;
    private readonly ILogger<NominatimReverseGeocoder> _logger;

    /// <summary>Initializes a new instance of the <see cref="NominatimReverseGeocoder"/> class.</summary>
    public NominatimReverseGeocoder(HttpClient http, ILogger<NominatimReverseGeocoder> logger)
    {
        _http = http;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ResolvedGeoPlace?> ReverseAsync(
        double latitude,
        double longitude,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var url =
                $"reverse?format=jsonv2&lat={latitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}&lon={longitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}&zoom=10&addressdetails=1";
            var payload = await _http.GetFromJsonAsync<NominatimResponse>(url, cancellationToken);
            var address = payload?.Address;
            if (address is null)
            {
                return null;
            }

            var city = FirstNonEmpty(address.City, address.Town, address.Village, address.Municipality, address.County);
            var state = FirstNonEmpty(address.State, address.Region, address.County);
            var country = address.Country;
            var iso2 = address.CountryCode;
            if (string.IsNullOrWhiteSpace(city) || string.IsNullOrWhiteSpace(state) || string.IsNullOrWhiteSpace(country) ||
                string.IsNullOrWhiteSpace(iso2))
            {
                return null;
            }

            var iso2Trim = iso2.Trim().ToUpperInvariant();
            return new ResolvedGeoPlace(
                country.Trim(),
                iso2Trim,
                iso2Trim.Length == 2 ? iso2Trim + "X" : iso2Trim,
                state.Trim(),
                address.StateCode,
                city.Trim());
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Nominatim reverse geocode failed for {Lat},{Lng}", latitude, longitude);
            return null;
        }
    }

    private static string? FirstNonEmpty(params string?[] values) =>
        values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));

    private sealed class NominatimResponse
    {
        [JsonPropertyName("address")]
        public NominatimAddress? Address { get; set; }
    }

    private sealed class NominatimAddress
    {
        [JsonPropertyName("city")]
        public string? City { get; set; }

        [JsonPropertyName("town")]
        public string? Town { get; set; }

        [JsonPropertyName("village")]
        public string? Village { get; set; }

        [JsonPropertyName("municipality")]
        public string? Municipality { get; set; }

        [JsonPropertyName("county")]
        public string? County { get; set; }

        [JsonPropertyName("state")]
        public string? State { get; set; }

        [JsonPropertyName("region")]
        public string? Region { get; set; }

        [JsonPropertyName("ISO3166-2-lvl4")]
        public string? StateCode { get; set; }

        [JsonPropertyName("country")]
        public string? Country { get; set; }

        [JsonPropertyName("country_code")]
        public string? CountryCode { get; set; }
    }
}

/// <summary>No-op geocoder for tests and offline fallback to nearest directory city.</summary>
public sealed class NullReverseGeocoder : IReverseGeocoder
{
    /// <inheritdoc />
    public Task<ResolvedGeoPlace?> ReverseAsync(
        double latitude,
        double longitude,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<ResolvedGeoPlace?>(null);
}
