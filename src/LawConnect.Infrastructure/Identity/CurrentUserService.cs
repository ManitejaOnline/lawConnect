using System.Security.Claims;
using LawConnect.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;

namespace LawConnect.Infrastructure.Identity;

/// <summary>
/// Resolves the current user from <see cref="HttpContext"/>.
/// </summary>
public sealed class CurrentUserService : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="CurrentUserService"/> class.
    /// </summary>
    /// <param name="httpContextAccessor">HTTP context accessor.</param>
    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    /// <inheritdoc />
    public string? UserId =>
        User?.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? User?.FindFirstValue("sub");

    /// <inheritdoc />
    public string? UserName =>
        User?.FindFirstValue(ClaimTypes.Name)
        ?? User?.Identity?.Name;

    /// <inheritdoc />
    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;

    /// <inheritdoc />
    public IReadOnlyCollection<string> Roles =>
        User?.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray()
        ?? Array.Empty<string>();
}
