using LawConnect.Domain.Entities;
using LawConnect.Shared.Constants;
using Microsoft.EntityFrameworkCore;

namespace LawConnect.Infrastructure.Persistence.Seeding;

/// <summary>
/// Ensures Country/State/City directory rows exist for SOS geo matching.
/// Reference data only — matching still resolves customer GPS via ILocationResolver.
/// </summary>
public static class GeographySeeder
{
    /// <summary>Inserts missing countries, states, and cities.</summary>
    public static void Ensure(ApplicationDbContext db)
    {
        EnsureCountry(db, SeedIds.Geography.India, "India", "IN", "IND", "+91");
        EnsureCountry(db, SeedIds.Geography.UnitedKingdom, "United Kingdom", "GB", "GBR", "+44");

        EnsureState(db, SeedIds.Geography.Karnataka, SeedIds.Geography.India, "Karnataka", "KA");
        EnsureState(db, SeedIds.Geography.Maharashtra, SeedIds.Geography.India, "Maharashtra", "MH");
        EnsureState(db, SeedIds.Geography.Delhi, SeedIds.Geography.India, "Delhi", "DL");
        EnsureState(db, SeedIds.Geography.Telangana, SeedIds.Geography.India, "Telangana", "TG");
        EnsureState(db, SeedIds.Geography.Haryana, SeedIds.Geography.India, "Haryana", "HR");
        EnsureState(db, SeedIds.Geography.Chandigarh, SeedIds.Geography.India, "Chandigarh", "CH");
        EnsureState(db, SeedIds.Geography.Gujarat, SeedIds.Geography.India, "Gujarat", "GJ");
        EnsureState(db, SeedIds.Geography.Rajasthan, SeedIds.Geography.India, "Rajasthan", "RJ");
        EnsureState(db, SeedIds.Geography.England, SeedIds.Geography.UnitedKingdom, "England", "ENG");

        EnsureCity(db, SeedIds.Geography.Bengaluru, SeedIds.Geography.Karnataka, "Bengaluru", 12.9716, 77.5946);
        EnsureCity(db, SeedIds.Geography.Mysuru, SeedIds.Geography.Karnataka, "Mysuru", 12.2958, 76.6394);
        EnsureCity(db, SeedIds.Geography.Mumbai, SeedIds.Geography.Maharashtra, "Mumbai", 19.0760, 72.8777);
        EnsureCity(db, SeedIds.Geography.NewDelhi, SeedIds.Geography.Delhi, "New Delhi", 28.6139, 77.2090);
        EnsureCity(db, SeedIds.Geography.Hyderabad, SeedIds.Geography.Telangana, "Hyderabad", 17.3850, 78.4867);
        EnsureCity(db, SeedIds.Geography.Gurugram, SeedIds.Geography.Haryana, "Gurugram", 28.4595, 77.0266);
        EnsureCity(db, SeedIds.Geography.ChandigarhCity, SeedIds.Geography.Chandigarh, "Chandigarh", 30.7333, 76.7794);
        EnsureCity(db, SeedIds.Geography.Surat, SeedIds.Geography.Gujarat, "Surat", 21.1702, 72.8311);
        EnsureCity(db, SeedIds.Geography.Jaipur, SeedIds.Geography.Rajasthan, "Jaipur", 26.9124, 75.7873);
        EnsureCity(db, SeedIds.Geography.Ahmedabad, SeedIds.Geography.Gujarat, "Ahmedabad", 23.0225, 72.5714);
        EnsureCity(db, SeedIds.Geography.London, SeedIds.Geography.England, "London", 51.5074, -0.1278);

        db.SaveChanges();
        BackfillAddressCities(db);
    }

    private static void EnsureCountry(ApplicationDbContext db, Guid id, string name, string iso2, string iso3, string phone)
    {
        if (db.Countries.Any(c => c.Id == id || c.Iso2Code == iso2))
        {
            return;
        }

        db.Countries.Add(new Country
        {
            Id = id,
            Name = name,
            Iso2Code = iso2,
            Iso3Code = iso3,
            PhoneCode = phone,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
    }

    private static void EnsureState(ApplicationDbContext db, Guid id, Guid countryId, string name, string code)
    {
        if (db.States.Any(s => s.Id == id || (s.CountryId == countryId && s.Name == name)))
        {
            return;
        }

        db.States.Add(new State
        {
            Id = id,
            CountryId = countryId,
            Name = name,
            Code = code,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
    }

    private static void EnsureCity(ApplicationDbContext db, Guid id, Guid stateId, string name, double lat, double lng)
    {
        var existing = db.Cities.FirstOrDefault(c => c.Id == id || (c.StateId == stateId && c.Name == name));
        if (existing is not null)
        {
            existing.Latitude ??= lat;
            existing.Longitude ??= lng;
            return;
        }

        db.Cities.Add(new City
        {
            Id = id,
            StateId = stateId,
            Name = name,
            Latitude = lat,
            Longitude = lng,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
    }

    private static void BackfillAddressCities(ApplicationDbContext db)
    {
        var cities = db.Cities
            .Where(c => c.Latitude != null && c.Longitude != null)
            .Select(c => new { c.Id, Lat = c.Latitude!.Value, Lng = c.Longitude!.Value })
            .ToList();
        if (cities.Count == 0)
        {
            return;
        }

        var addresses = db.Addresses
            .Where(a => a.CityId == null && a.Latitude != null && a.Longitude != null)
            .ToList();
        foreach (var address in addresses)
        {
            var nearest = cities
                .Select(c => new
                {
                    c.Id,
                    Distance = Application.Common.Geo.GeoDistance.Kilometers(
                        address.Latitude!.Value,
                        address.Longitude!.Value,
                        c.Lat,
                        c.Lng)
                })
                .OrderBy(x => x.Distance)
                .First();
            if (nearest.Distance <= 150)
            {
                address.CityId = nearest.Id;
            }
        }

        db.SaveChanges();
    }
}
