namespace LawConnect.Application.Common.Models;

/// <summary>
/// Sorting parameters for list queries.
/// </summary>
public sealed class SortRequest
{
    /// <summary>
    /// Gets or sets the field name to sort by.
    /// </summary>
    public string? SortBy { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether sorting is descending.
    /// </summary>
    public bool Descending { get; set; }
}
