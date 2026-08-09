namespace LawConnect.Application.Common.Interfaces;

/// <summary>
/// Provides identity information for the current request principal.
/// </summary>
public interface ICurrentUser
{
    /// <summary>
    /// Gets the current user identifier, if authenticated.
    /// </summary>
    string? UserId { get; }

    /// <summary>
    /// Gets the current user display name, if available.
    /// </summary>
    string? UserName { get; }

    /// <summary>
    /// Gets a value indicating whether the current principal is authenticated.
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// Gets the roles assigned to the current principal.
    /// </summary>
    IReadOnlyCollection<string> Roles { get; }
}
