using LawConnect.Application.Common.Geo;
using LawConnect.Application.Common.Interfaces;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Lawyers.Models;
using LawConnect.Application.Features.Lawyers.Services;
using LawConnect.Domain.Entities;
using LawConnect.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LawConnect.Application.Features.Lawyers.Queries.SearchLawyers;

/// <summary>
/// Handles optimized lawyer discovery search.
/// </summary>
public sealed class SearchLawyersQueryHandler
    : IRequestHandler<SearchLawyersQuery, Result<PagedResult<LawyerSearchItemResponse>>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ILawyerSearchCache _cache;
    private readonly ILogger<SearchLawyersQueryHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SearchLawyersQueryHandler"/> class.
    /// </summary>
    public SearchLawyersQueryHandler(
        IApplicationDbContext dbContext,
        ILawyerSearchCache cache,
        ILogger<SearchLawyersQueryHandler> logger)
    {
        _dbContext = dbContext;
        _cache = cache;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<PagedResult<LawyerSearchItemResponse>>> Handle(
        SearchLawyersQuery request,
        CancellationToken cancellationToken)
    {
        var cacheKey = _cache.BuildKey(request);
        var cached = await _cache.GetAsync(cacheKey, cancellationToken);
        if (cached is not null)
        {
            return Result.Success(cached);
        }

        var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
        var pageSize = Math.Clamp(request.PageSize, 1, PaginationRequest.MaxPageSize);
        var requiresDistance = request.OriginLatitude.HasValue
            && request.OriginLongitude.HasValue
            && (request.MaxDistanceKm.HasValue
                || string.Equals(request.SortBy, LawyerSearchSortFields.Distance, StringComparison.OrdinalIgnoreCase));

        var query = BuildFilteredQuery(request);

        List<LawyerSearchItemResponse> items;
        int totalCount;

        if (requiresDistance)
        {
            var lawyers = await LoadLawyersAsync(query, cancellationToken);
            var originLat = request.OriginLatitude!.Value;
            var originLon = request.OriginLongitude!.Value;

            var withDistance = lawyers
                .Select(l => Map(l, ComputeDistance(l, originLat, originLon)))
                .Where(r => !request.MaxDistanceKm.HasValue
                    || (r.DistanceKm.HasValue && r.DistanceKm.Value <= request.MaxDistanceKm.Value))
                .ToList();

            withDistance = ApplyInMemorySort(withDistance, request.SortBy, request.SortDescending);
            totalCount = withDistance.Count;
            items = withDistance
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();
        }
        else
        {
            var sorted = ApplyDbSort(query, request.SortBy, request.SortDescending);
            totalCount = await sorted.CountAsync(cancellationToken);
            var pageIds = await sorted
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(l => l.Id)
                .ToListAsync(cancellationToken);

            var pageLawyers = await LoadLawyersByIdsAsync(pageIds, cancellationToken);
            var byId = pageLawyers.ToDictionary(l => l.Id);
            items = pageIds
                .Where(byId.ContainsKey)
                .Select(id => Map(byId[id], null))
                .ToList();
        }

        var page = new PagedResult<LawyerSearchItemResponse>(items, totalCount, pageNumber, pageSize);
        await _cache.SetAsync(cacheKey, page, cancellationToken);
        _logger.LogDebug("Lawyer search returned {Count}/{Total} results", items.Count, totalCount);
        return Result.Success(page);
    }

    private IQueryable<Lawyer> BuildFilteredQuery(SearchLawyersQuery request)
    {
        var query = _dbContext.Lawyers.AsNoTracking()
            .Where(l => l.IsVerified && l.VerificationStatus == VerificationStatus.Approved);

        if (request.IsAcceptingAppointments.HasValue)
        {
            query = query.Where(l => l.IsAcceptingAppointments == request.IsAcceptingAppointments.Value);
        }
        else
        {
            query = query.Where(l => l.IsAcceptingAppointments);
        }

        if (request.IsOnline.HasValue)
        {
            query = query.Where(l => l.IsOnline == request.IsOnline.Value);
        }

        if (request.IsAvailableForEmergency.HasValue)
        {
            query = query.Where(l => l.IsAvailableForEmergency == request.IsAvailableForEmergency.Value);
        }

        if (request.MinRating.HasValue)
        {
            query = query.Where(l => l.RatingAverage >= request.MinRating.Value);
        }

        if (request.MinFee.HasValue)
        {
            query = query.Where(l => l.ConsultationFee >= request.MinFee.Value);
        }

        if (request.MaxFee.HasValue)
        {
            query = query.Where(l => l.ConsultationFee <= request.MaxFee.Value);
        }

        if (request.MinExperience.HasValue)
        {
            query = query.Where(l => l.YearsOfExperience >= request.MinExperience.Value);
        }

        if (request.SpecializationId.HasValue)
        {
            var specializationId = request.SpecializationId.Value;
            query = query.Where(l => l.LawyerSpecializations.Any(s => s.SpecializationId == specializationId));
        }

        if (!string.IsNullOrWhiteSpace(request.LanguageCode))
        {
            var languageCode = request.LanguageCode.Trim().ToLowerInvariant();
            query = query.Where(l => l.Languages.Any(lang => lang.LanguageCode == languageCode));
        }

        if (request.AvailableOnDay.HasValue)
        {
            var day = request.AvailableOnDay.Value;
            var from = request.AvailableFrom;
            var to = request.AvailableTo;
            query = query.Where(l => l.Availabilities.Any(a =>
                a.IsAvailable
                && a.DayOfWeek == day
                && (!from.HasValue || a.StartTime <= from.Value)
                && (!to.HasValue || a.EndTime >= to.Value)));
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim().ToLower();
            query = query.Where(l =>
                l.User.FirstName.ToLower().Contains(term)
                || l.User.LastName.ToLower().Contains(term)
                || (l.Bio != null && l.Bio.ToLower().Contains(term))
                || l.LawyerSpecializations.Any(s => s.Specialization.Name.ToLower().Contains(term))
                || l.Languages.Any(lang => lang.LanguageName.ToLower().Contains(term)));
        }

        return query;
    }

    private async Task<List<Lawyer>> LoadLawyersAsync(
        IQueryable<Lawyer> query,
        CancellationToken cancellationToken)
    {
        var ids = await query.Select(l => l.Id).ToListAsync(cancellationToken);
        return await LoadLawyersByIdsAsync(ids, cancellationToken);
    }

    private async Task<List<Lawyer>> LoadLawyersByIdsAsync(
        IReadOnlyList<Guid> ids,
        CancellationToken cancellationToken)
    {
        if (ids.Count == 0)
        {
            return [];
        }

        return await _dbContext.Lawyers
            .AsNoTracking()
            .Include(l => l.User)
            .ThenInclude(u => u.Addresses)
            .Include(l => l.LawyerSpecializations)
            .ThenInclude(s => s.Specialization)
            .Include(l => l.Languages)
            .Where(l => ids.Contains(l.Id))
            .ToListAsync(cancellationToken);
    }

    private static double? ComputeDistance(Lawyer lawyer, double originLat, double originLon)
    {
        var address = lawyer.User.Addresses
            .FirstOrDefault(a => a.IsPrimary && a.Latitude.HasValue && a.Longitude.HasValue);
        return address is null
            ? null
            : GeoDistance.Kilometers(originLat, originLon, address.Latitude!.Value, address.Longitude!.Value);
    }

    private static LawyerSearchItemResponse Map(Lawyer lawyer, double? distanceKm)
    {
        var address = lawyer.User.Addresses
            .FirstOrDefault(a => a.IsPrimary && a.Latitude.HasValue && a.Longitude.HasValue);

        return new LawyerSearchItemResponse(
            lawyer.Id,
            lawyer.UserId,
            lawyer.User.FirstName,
            lawyer.User.LastName,
            lawyer.User.ProfileImageUrl,
            lawyer.Bio,
            lawyer.YearsOfExperience,
            lawyer.ConsultationFee,
            lawyer.EmergencyFee,
            lawyer.IsVerified,
            lawyer.IsOnline,
            lawyer.IsAcceptingAppointments,
            lawyer.IsAvailableForEmergency,
            lawyer.RatingAverage,
            lawyer.RatingCount,
            lawyer.VerificationStatus.ToString(),
            lawyer.LawyerSpecializations
                .OrderByDescending(s => s.IsPrimary)
                .Select(s => s.Specialization.Name)
                .ToArray(),
            lawyer.Languages.Select(l => l.LanguageName).ToArray(),
            address?.Latitude,
            address?.Longitude,
            distanceKm);
    }

    private static IQueryable<Lawyer> ApplyDbSort(IQueryable<Lawyer> query, string? sortBy, bool descending)
    {
        return (sortBy?.ToLowerInvariant()) switch
        {
            LawyerSearchSortFields.Rating => descending
                ? query.OrderByDescending(x => x.RatingAverage).ThenByDescending(x => x.RatingCount)
                : query.OrderBy(x => x.RatingAverage).ThenBy(x => x.RatingCount),
            LawyerSearchSortFields.Fee => descending
                ? query.OrderByDescending(x => x.ConsultationFee)
                : query.OrderBy(x => x.ConsultationFee),
            LawyerSearchSortFields.Experience => descending
                ? query.OrderByDescending(x => x.YearsOfExperience)
                : query.OrderBy(x => x.YearsOfExperience),
            _ => descending
                ? query.OrderByDescending(x => x.CreatedAt)
                : query.OrderByDescending(x => x.RatingAverage).ThenByDescending(x => x.CreatedAt)
        };
    }

    private static List<LawyerSearchItemResponse> ApplyInMemorySort(
        List<LawyerSearchItemResponse> items,
        string? sortBy,
        bool descending)
    {
        IOrderedEnumerable<LawyerSearchItemResponse> ordered = (sortBy?.ToLowerInvariant()) switch
        {
            LawyerSearchSortFields.Distance => descending
                ? items.OrderByDescending(x => x.DistanceKm ?? double.MaxValue)
                : items.OrderBy(x => x.DistanceKm ?? double.MaxValue),
            LawyerSearchSortFields.Fee => descending
                ? items.OrderByDescending(x => x.ConsultationFee)
                : items.OrderBy(x => x.ConsultationFee),
            LawyerSearchSortFields.Experience => descending
                ? items.OrderByDescending(x => x.YearsOfExperience)
                : items.OrderBy(x => x.YearsOfExperience),
            LawyerSearchSortFields.Rating => descending
                ? items.OrderByDescending(x => x.RatingAverage)
                : items.OrderBy(x => x.RatingAverage),
            _ => items.OrderBy(x => x.DistanceKm ?? double.MaxValue)
        };

        return ordered.ToList();
    }
}
