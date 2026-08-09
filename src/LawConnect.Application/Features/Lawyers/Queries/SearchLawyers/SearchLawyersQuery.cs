using FluentValidation;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Lawyers.Models;
using LawConnect.Domain.Enums;
using MediatR;

namespace LawConnect.Application.Features.Lawyers.Queries.SearchLawyers;

/// <summary>
/// Searches lawyers with filters, sorting, and server-side pagination.
/// </summary>
public sealed record SearchLawyersQuery(
    string? Search,
    Guid? SpecializationId,
    string? LanguageCode,
    double? MinRating,
    decimal? MinFee,
    decimal? MaxFee,
    int? MinExperience,
    bool? IsOnline,
    bool? IsAvailableForEmergency,
    bool? IsAcceptingAppointments,
    DayOfWeekFlag? AvailableOnDay,
    TimeOnly? AvailableFrom,
    TimeOnly? AvailableTo,
    double? OriginLatitude,
    double? OriginLongitude,
    double? MaxDistanceKm,
    string? SortBy,
    bool SortDescending,
    int PageNumber,
    int PageSize) : IRequest<Result<PagedResult<LawyerSearchItemResponse>>>;

/// <summary>
/// Validates <see cref="SearchLawyersQuery"/>.
/// </summary>
public sealed class SearchLawyersQueryValidator : AbstractValidator<SearchLawyersQuery>
{
    private static readonly HashSet<string> AllowedSort = new(StringComparer.OrdinalIgnoreCase)
    {
        LawyerSearchSortFields.Rating,
        LawyerSearchSortFields.Fee,
        LawyerSearchSortFields.Experience,
        LawyerSearchSortFields.Distance,
        LawyerSearchSortFields.Relevance
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="SearchLawyersQueryValidator"/> class.
    /// </summary>
    public SearchLawyersQueryValidator()
    {
        RuleFor(x => x.Search).MaximumLength(100).When(x => !string.IsNullOrWhiteSpace(x.Search));
        RuleFor(x => x.LanguageCode).MaximumLength(16).When(x => !string.IsNullOrWhiteSpace(x.LanguageCode));
        RuleFor(x => x.MinRating).InclusiveBetween(0, 5).When(x => x.MinRating.HasValue);
        RuleFor(x => x.MinFee).GreaterThanOrEqualTo(0).When(x => x.MinFee.HasValue);
        RuleFor(x => x.MaxFee).GreaterThanOrEqualTo(0).When(x => x.MaxFee.HasValue);
        RuleFor(x => x).Must(x => !x.MinFee.HasValue || !x.MaxFee.HasValue || x.MaxFee >= x.MinFee)
            .WithMessage("MaxFee must be greater than or equal to MinFee.");
        RuleFor(x => x.MinExperience).InclusiveBetween(0, 80).When(x => x.MinExperience.HasValue);
        RuleFor(x => x.AvailableOnDay).IsInEnum().When(x => x.AvailableOnDay.HasValue);
        RuleFor(x => x).Must(x => !x.AvailableFrom.HasValue || !x.AvailableTo.HasValue || x.AvailableTo > x.AvailableFrom)
            .WithMessage("AvailableTo must be after AvailableFrom.");
        RuleFor(x => x.OriginLatitude).InclusiveBetween(-90, 90).When(x => x.OriginLatitude.HasValue);
        RuleFor(x => x.OriginLongitude).InclusiveBetween(-180, 180).When(x => x.OriginLongitude.HasValue);
        RuleFor(x => x.MaxDistanceKm).GreaterThan(0).When(x => x.MaxDistanceKm.HasValue);
        RuleFor(x => x).Must(x =>
                (!x.OriginLatitude.HasValue && !x.OriginLongitude.HasValue)
                || (x.OriginLatitude.HasValue && x.OriginLongitude.HasValue))
            .WithMessage("Both OriginLatitude and OriginLongitude are required for distance filtering.");
        RuleFor(x => x.SortBy)
            .Must(s => string.IsNullOrWhiteSpace(s) || AllowedSort.Contains(s))
            .WithMessage("Unsupported sort field.");
        RuleFor(x => x.PageNumber).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, PaginationRequest.MaxPageSize);
    }
}
