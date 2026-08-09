namespace LawConnect.Domain.Exceptions;

/// <summary>
/// Thrown when a requested resource cannot be found.
/// </summary>
public sealed class NotFoundException : DomainException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NotFoundException"/> class.
    /// </summary>
    /// <param name="message">Error message.</param>
    public NotFoundException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NotFoundException"/> class.
    /// </summary>
    /// <param name="entityName">Entity type name.</param>
    /// <param name="key">Entity key.</param>
    public NotFoundException(string entityName, object key)
        : base($"{entityName} with key '{key}' was not found.")
    {
        EntityName = entityName;
        Key = key;
    }

    /// <summary>
    /// Gets the entity type name when available.
    /// </summary>
    public string? EntityName { get; }

    /// <summary>
    /// Gets the entity key when available.
    /// </summary>
    public object? Key { get; }
}
