using LawConnect.Domain.Exceptions;

namespace LawConnect.Application.Common.Interfaces;

/// <summary>
/// Extensions for <see cref="ICurrentUser"/>.
/// </summary>
public static class CurrentUserExtensions
{
    /// <summary>
    /// Returns the authenticated user id or throws.
    /// </summary>
    /// <param name="currentUser">Current user accessor.</param>
    /// <returns>User id.</returns>
    /// <exception cref="UnauthorizedException">Thrown when not authenticated.</exception>
    public static Guid GetRequiredUserId(this ICurrentUser currentUser)
    {
        if (!currentUser.IsAuthenticated
            || string.IsNullOrWhiteSpace(currentUser.UserId)
            || !Guid.TryParse(currentUser.UserId, out var userId))
        {
            throw new UnauthorizedException("Authentication is required.");
        }

        return userId;
    }
}
