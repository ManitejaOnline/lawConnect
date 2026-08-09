using LawConnect.Application.Common.Models;
using LawConnect.Application.Common.Specifications;
using Microsoft.EntityFrameworkCore;

namespace LawConnect.Application.Common.Extensions;

/// <summary>
/// Queryable helpers for specifications and paging.
/// </summary>
public static class QueryableExtensions
{
    /// <summary>
    /// Applies a specification to a queryable source.
    /// </summary>
    /// <typeparam name="T">Entity type.</typeparam>
    /// <param name="query">Source query.</param>
    /// <param name="specification">Specification to apply.</param>
    /// <returns>The filtered query.</returns>
    public static IQueryable<T> ApplySpecification<T>(
        this IQueryable<T> query,
        ISpecification<T> specification)
        where T : class
    {
        return SpecificationEvaluator.GetQuery(query, specification);
    }

    /// <summary>
    /// Materializes a paged result from a queryable source.
    /// </summary>
    /// <typeparam name="T">Item type.</typeparam>
    /// <param name="query">Source query.</param>
    /// <param name="pagination">Pagination parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A paged result.</returns>
    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
        this IQueryable<T> query,
        PaginationRequest pagination,
        CancellationToken cancellationToken = default)
    {
        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip(pagination.Skip)
            .Take(pagination.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<T>(items, totalCount, pagination.PageNumber, pagination.PageSize);
    }
}
