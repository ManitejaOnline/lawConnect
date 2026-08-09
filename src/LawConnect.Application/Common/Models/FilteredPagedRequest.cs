namespace LawConnect.Application.Common.Models;

/// <summary>
/// Combined pagination, search, and sort request.
/// </summary>
public class FilteredPagedRequest : PaginationRequest
{
    /// <summary>
    /// Gets or sets an optional free-text search term.
    /// </summary>
    public string? Search { get; set; }

    /// <summary>
    /// Gets or sets sort options.
    /// </summary>
    public SortRequest Sort { get; set; } = new();
}
