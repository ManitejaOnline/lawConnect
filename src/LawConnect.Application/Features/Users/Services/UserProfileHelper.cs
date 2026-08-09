using LawConnect.Application.Common.Interfaces;
using LawConnect.Application.Features.Users.Models;
using LawConnect.Domain.Entities;
using LawConnect.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace LawConnect.Application.Features.Users.Services;

/// <summary>
/// Shared user profile load helpers.
/// </summary>
public static class UserProfileHelper
{
    /// <summary>
    /// Loads a user with customer, preference, and roles; creates missing customer/preference rows.
    /// </summary>
    public static async Task<User> GetOrCreateProfileGraphAsync(
        IApplicationDbContext dbContext,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var user = await dbContext.Users
            .Include(u => u.Customer)
            .Include(u => u.NotificationPreference)
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new NotFoundException(nameof(User), userId);

        var changed = false;
        if (user.Customer is null)
        {
            user.Customer = new Customer { UserId = user.Id };
            dbContext.Customers.Add(user.Customer);
            changed = true;
        }

        if (user.NotificationPreference is null)
        {
            user.NotificationPreference = new NotificationPreference { UserId = user.Id };
            dbContext.NotificationPreferences.Add(user.NotificationPreference);
            changed = true;
        }

        if (changed)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return user;
    }

    /// <summary>
    /// Maps a user graph to <see cref="UserProfileResponse"/>.
    /// </summary>
    public static UserProfileResponse ToProfileResponse(User user) =>
        new(
            user.Id,
            user.PhoneNumber,
            user.Email,
            user.FirstName,
            user.LastName,
            user.ProfileImageUrl,
            user.IsPhoneVerified,
            user.Customer?.DateOfBirth,
            user.Customer?.Gender,
            user.Customer?.PreferredLanguage,
            user.UserRoles.Select(ur => ur.Role.Name).Distinct(StringComparer.OrdinalIgnoreCase).ToArray());
}
