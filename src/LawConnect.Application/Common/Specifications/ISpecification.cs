using System.Linq.Expressions;

namespace LawConnect.Application.Common.Specifications;

/// <summary>
/// Encapsulates query criteria, includes, ordering, and paging.
/// </summary>
/// <typeparam name="T">Entity type.</typeparam>
public interface ISpecification<T>
{
    /// <summary>Gets the filter expression.</summary>
    Expression<Func<T, bool>>? Criteria { get; }

    /// <summary>Gets strongly typed include expressions.</summary>
    IReadOnlyList<Expression<Func<T, object>>> Includes { get; }

    /// <summary>Gets string-based include paths.</summary>
    IReadOnlyList<string> IncludeStrings { get; }

    /// <summary>Gets the ascending order expression.</summary>
    Expression<Func<T, object>>? OrderBy { get; }

    /// <summary>Gets the descending order expression.</summary>
    Expression<Func<T, object>>? OrderByDescending { get; }

    /// <summary>Gets the page size when paging is enabled.</summary>
    int Take { get; }

    /// <summary>Gets the skip count when paging is enabled.</summary>
    int Skip { get; }

    /// <summary>Gets a value indicating whether paging is enabled.</summary>
    bool IsPagingEnabled { get; }

    /// <summary>Gets a value indicating whether the query should use no-tracking.</summary>
    bool AsNoTracking { get; }
}
