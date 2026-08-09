namespace LawConnect.Application.Common.Models;

/// <summary>
/// Pagination parameters with safe defaults and bounds.
/// </summary>
public class PaginationRequest
{
    /// <summary>Default page number.</summary>
    public const int DefaultPageNumber = 1;

    /// <summary>Default page size.</summary>
    public const int DefaultPageSize = 20;

    /// <summary>Maximum allowed page size.</summary>
    public const int MaxPageSize = 100;

    private int _pageNumber = DefaultPageNumber;
    private int _pageSize = DefaultPageSize;

    /// <summary>
    /// Gets or sets the 1-based page number.
    /// </summary>
    public int PageNumber
    {
        get => _pageNumber;
        set => _pageNumber = value < 1 ? DefaultPageNumber : value;
    }

    /// <summary>
    /// Gets or sets the page size, capped at <see cref="MaxPageSize"/>.
    /// </summary>
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value switch
        {
            < 1 => DefaultPageSize,
            > MaxPageSize => MaxPageSize,
            _ => value
        };
    }

    /// <summary>
    /// Gets the number of items to skip for the current page.
    /// </summary>
    public int Skip => (PageNumber - 1) * PageSize;
}
