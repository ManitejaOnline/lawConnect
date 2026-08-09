using LawConnect.Domain.Enums;

namespace LawConnect.Application.Features.Emergency.Services;

/// <summary>Customer-facing SOS search copy. No km/radius language.</summary>
public static class EmergencySearchCopy
{
    /// <summary>Headline while searching/notifying in a geographic group.</summary>
    public static string Searching(EmergencySearchScope scope, string? city, string? state, string? country) =>
        scope switch
        {
            EmergencySearchScope.City when !string.IsNullOrWhiteSpace(city) =>
                $"Searching for an emergency lawyer in {city}...",
            EmergencySearchScope.City =>
                "Searching for an emergency lawyer in your city...",
            EmergencySearchScope.State when !string.IsNullOrWhiteSpace(state) =>
                $"Expanding search to {state}...",
            EmergencySearchScope.State =>
                "Expanding search to your state...",
            EmergencySearchScope.Country when !string.IsNullOrWhiteSpace(country) =>
                $"Searching for available lawyers across {country}...",
            EmergencySearchScope.Country =>
                "Searching for available lawyers across your country...",
            EmergencySearchScope.International =>
                "Searching for available lawyers internationally...",
            _ => "Searching for an emergency lawyer..."
        };

    /// <summary>Timeline line when the current group has nobody to notify.</summary>
    public static string NoLawyersExpanding(
        EmergencySearchScope from,
        EmergencySearchScope to,
        string? city,
        string? state,
        string? country)
    {
        var fromLabel = from switch
        {
            EmergencySearchScope.City => string.IsNullOrWhiteSpace(city) ? "your city" : city,
            EmergencySearchScope.State => string.IsNullOrWhiteSpace(state) ? "your state" : state,
            _ => string.IsNullOrWhiteSpace(country) ? "your country" : country
        };
        var next = Searching(to, city, state, country);
        return $"No available lawyer in {fromLabel}. {next}";
    }

    /// <summary>Short label for the current group (API + mobile).</summary>
    public static string? ScopeLabel(EmergencySearchScope scope, string? city, string? state, string? country) =>
        scope switch
        {
            EmergencySearchScope.City => city,
            EmergencySearchScope.State => state,
            EmergencySearchScope.Country => country,
            _ => null
        };
}
