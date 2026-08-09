namespace LawConnect.Application.Common.Models;

/// <summary>
/// Represents a page of items with pagination metadata.
/// </summary>
/// <typeparam name="T">Item type.</typeparam>
public sealed class PagedResult<T>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PagedResult{T}"/> class.
    /// </summary>
    /// <param name="items">Page items.</param>
    /// <param name="totalCount">Total matching item count.</param>
    /// <param name="pageNumber">Current page number (1-based).</param>
    /// <param name="pageSize">Page size.</param>
    public PagedResult(IReadOnlyList<T> items, int totalCount, int pageNumber, int pageSize)
    {
        Items = items;
        TotalCount = totalCount;
        PageNumber = pageNumber;
        PageSize = pageSize;
    }

    /// <summary>Gets the items in the current page.</summary>
    public IReadOnlyList<T> Items { get; }

    /// <summary>Gets the total number of matching items.</summary>
    public int TotalCount { get; }

    /// <summary>Gets the current page number (1-based).</summary>
    public int PageNumber { get; }

    /// <summary>Gets the page size.</summary>
    public int PageSize { get; }

    /// <summary>Gets the total number of pages.</summary>
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);

    /// <summary>Gets a value indicating whether a previous page exists.</summary>
    public bool HasPreviousPage => PageNumber > 1;

    /// <summary>Gets a value indicating whether a next page exists.</summary>
    public bool HasNextPage => PageNumber < TotalPages;

    /// <summary>
    /// Creates an empty paged result.
    /// </summary>
    /// <param name="pageNumber">Page number.</param>
    /// <param name="pageSize">Page size.</param>
    /// <returns>An empty paged result.</returns>
    public static PagedResult<T> Empty(int pageNumber, int pageSize) =>
        new([], 0, pageNumber, pageSize);
}
