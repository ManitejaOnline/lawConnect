using LawConnect.Application.Common.Specifications;
using Microsoft.EntityFrameworkCore;

namespace LawConnect.Application.Common.Extensions;

/// <summary>
/// Applies <see cref="ISpecification{T}"/> rules to an <see cref="IQueryable{T}"/>.
/// </summary>
public static class SpecificationEvaluator
{
    /// <summary>
    /// Builds a query from the provided specification.
    /// </summary>
    /// <typeparam name="T">Entity type.</typeparam>
    /// <param name="inputQuery">Source query.</param>
    /// <param name="specification">Specification to apply.</param>
    /// <returns>The composed query.</returns>
    public static IQueryable<T> GetQuery<T>(
        IQueryable<T> inputQuery,
        ISpecification<T> specification)
        where T : class
    {
        var query = inputQuery;

        if (specification.AsNoTracking)
        {
            query = query.AsNoTracking();
        }

        if (specification.Criteria is not null)
        {
            query = query.Where(specification.Criteria);
        }

        query = specification.Includes.Aggregate(
            query,
            (current, include) => current.Include(include));

        query = specification.IncludeStrings.Aggregate(
            query,
            (current, include) => current.Include(include));

        if (specification.OrderBy is not null)
        {
            query = query.OrderBy(specification.OrderBy);
        }
        else if (specification.OrderByDescending is not null)
        {
            query = query.OrderByDescending(specification.OrderByDescending);
        }

        if (specification.IsPagingEnabled)
        {
            query = query.Skip(specification.Skip).Take(specification.Take);
        }

        return query;
    }
}
