using LawConnect.Application.Common.Geo;
using LawConnect.Application.Common.Interfaces;
using LawConnect.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LawConnect.Application.Features.Emergency.Services;

/// <summary>
/// Finds and ranks eligible emergency lawyers by geographic group.
/// </summary>
public interface IEmergencyLawyerMatcher
{
    /// <summary>
    /// Returns ranked eligible lawyers in the given geographic group.
    /// GPS distance is informational only and does not gate eligibility.
    /// </summary>
    Task<IReadOnlyList<RankedEmergencyLawyer>> FindRankedAsync(
        Guid specializationId,
        double latitude,
        double longitude,
        Guid? customerCityId,
        Guid? customerStateId,
        Guid? customerCountryId,
        EmergencySearchScope scope,
        int take,
        IReadOnlyCollection<Guid> excludeLawyerIds,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Default SOS lawyer matcher with geographic grouping and weighted ranking.
/// </summary>
public sealed class EmergencyLawyerMatcher : IEmergencyLawyerMatcher
{
    private readonly IApplicationDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="EmergencyLawyerMatcher"/> class.
    /// </summary>
    public EmergencyLawyerMatcher(IApplicationDbContext dbContext) => _dbContext = dbContext;

    /// <inheritdoc />
    public async Task<IReadOnlyList<RankedEmergencyLawyer>> FindRankedAsync(
        Guid specializationId,
        double latitude,
        double longitude,
        Guid? customerCityId,
        Guid? customerStateId,
        Guid? customerCountryId,
        EmergencySearchScope scope,
        int take,
        IReadOnlyCollection<Guid> excludeLawyerIds,
        CancellationToken cancellationToken = default)
    {
        var busyLawyerIds = await _dbContext.EmergencyAssignments
            .AsNoTracking()
            .Where(a =>
                (a.Status == EmergencyAssignmentStatus.Accepted
                    && (a.EmergencyRequest.Status == EmergencyStatus.Assigned
                        || a.EmergencyRequest.Status == EmergencyStatus.Calling
                        || a.EmergencyRequest.Status == EmergencyStatus.Active
                        || a.EmergencyRequest.Status == EmergencyStatus.InProgress))
                || (a.Status == EmergencyAssignmentStatus.Pending
                    && (a.EmergencyRequest.Status == EmergencyStatus.Created
                        || a.EmergencyRequest.Status == EmergencyStatus.Searching
                        || a.EmergencyRequest.Status == EmergencyStatus.Notifying)))
            .Select(a => a.LawyerId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var exclude = excludeLawyerIds.Concat(busyLawyerIds).ToHashSet();

        var lawyers = _dbContext.Lawyers
            .AsNoTracking()
            .Where(l =>
                l.IsVerified
                && l.VerificationStatus == VerificationStatus.Approved
                && l.IsAvailableForEmergency
                && l.IsOnline
                && l.LawyerSpecializations.Any(s => s.SpecializationId == specializationId)
                && !exclude.Contains(l.Id)
                && l.User.Addresses.Any(a => a.IsPrimary && a.CityId != null));

        lawyers = scope switch
        {
            EmergencySearchScope.City when customerCityId.HasValue =>
                lawyers.Where(l => l.User.Addresses.Any(a => a.IsPrimary && a.CityId == customerCityId)),
            EmergencySearchScope.State when customerStateId.HasValue =>
                lawyers.Where(l => l.User.Addresses.Any(a =>
                    a.IsPrimary && a.City != null && a.City.StateId == customerStateId)),
            EmergencySearchScope.Country when customerCountryId.HasValue =>
                lawyers.Where(l => l.User.Addresses.Any(a =>
                    a.IsPrimary && a.City != null && a.City.State.CountryId == customerCountryId)),
            EmergencySearchScope.International when customerCountryId.HasValue =>
                lawyers.Where(l => l.User.Addresses.Any(a =>
                    a.IsPrimary && a.City != null && a.City.State.CountryId != customerCountryId)),
            _ => lawyers.Where(_ => false)
        };

        var candidates = await lawyers
            .Select(l => new
            {
                l.Id,
                l.UserId,
                l.RatingAverage,
                l.YearsOfExperience,
                Latitude = l.User.Addresses
                    .Where(a => a.IsPrimary && a.Latitude != null && a.Longitude != null)
                    .Select(a => (double?)a.Latitude)
                    .FirstOrDefault(),
                Longitude = l.User.Addresses
                    .Where(a => a.IsPrimary && a.Latitude != null && a.Longitude != null)
                    .Select(a => (double?)a.Longitude)
                    .FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        if (candidates.Count == 0)
        {
            return Array.Empty<RankedEmergencyLawyer>();
        }

        var lawyerIds = candidates.Select(x => x.Id).ToArray();
        var stats = await _dbContext.EmergencyAssignments
            .AsNoTracking()
            .Where(a => lawyerIds.Contains(a.LawyerId))
            .GroupBy(a => a.LawyerId)
            .Select(g => new
            {
                LawyerId = g.Key,
                Notified = g.Count(),
                Accepted = g.Count(x => x.Status == EmergencyAssignmentStatus.Accepted),
                AvgResponse = g.Where(x => x.ResponseTimeSeconds != null)
                    .Average(x => (double?)x.ResponseTimeSeconds) ?? 120d
            })
            .ToDictionaryAsync(x => x.LawyerId, cancellationToken);

        var ranked = candidates
            .Select(c =>
            {
                stats.TryGetValue(c.Id, out var s);
                var notified = s?.Notified ?? 0;
                var accepted = s?.Accepted ?? 0;
                var acceptanceRate = notified == 0 ? 0.5d : (double)accepted / notified;
                var avgResponse = s?.AvgResponse ?? 60d;
                var distanceKm = c.Latitude.HasValue && c.Longitude.HasValue
                    ? GeoDistance.Kilometers(latitude, longitude, c.Latitude.Value, c.Longitude.Value)
                    : 0d;
                var score = EmergencyLawyerRanker.Score(
                    c.RatingAverage,
                    acceptanceRate,
                    avgResponse,
                    c.YearsOfExperience);

                return new RankedEmergencyLawyer(
                    c.Id,
                    c.UserId,
                    distanceKm,
                    score,
                    c.RatingAverage,
                    acceptanceRate,
                    avgResponse,
                    c.YearsOfExperience);
            })
            .OrderByDescending(x => x.Score)
            .ThenByDescending(x => x.RatingAverage)
            .ThenByDescending(x => x.YearsOfExperience)
            .ThenByDescending(x => x.AcceptanceRate)
            .ThenBy(x => x.AverageResponseSeconds)
            .ThenBy(x => x.DistanceKm)
            .Take(take)
            .ToList();

        return ranked;
    }
}

/// <summary>
/// Weighted SOS ranking helper (pure functions for unit tests).
/// Distance is not part of the score — geographic group is chosen first.
/// </summary>
public static class EmergencyLawyerRanker
{
    /// <summary>
    /// Computes a composite score; higher is better.
    /// Order: rating, experience, acceptance rate, response time.
    /// </summary>
    public static double Score(
        double ratingAverage,
        double acceptanceRate,
        double averageResponseSeconds,
        int yearsOfExperience)
    {
        var ratingScore = Math.Clamp(ratingAverage / 5d, 0d, 1d);
        var experienceScore = Math.Clamp(yearsOfExperience / 30d, 0d, 1d);
        var acceptanceScore = Math.Clamp(acceptanceRate, 0d, 1d);
        var responseScore = 1d - Math.Clamp(averageResponseSeconds / 120d, 0d, 1d);

        return (ratingScore * 0.35)
               + (experienceScore * 0.25)
               + (acceptanceScore * 0.25)
               + (responseScore * 0.15);
    }
}
