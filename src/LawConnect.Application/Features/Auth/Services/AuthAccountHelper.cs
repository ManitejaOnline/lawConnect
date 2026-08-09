using LawConnect.Application.Common.Interfaces;
using LawConnect.Domain.Entities;
using LawConnect.Domain.Enums;
using LawConnect.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace LawConnect.Application.Features.Auth.Services;

/// <summary>
/// Shared helpers for password registration and login.
/// </summary>
public static class AuthAccountHelper
{
    /// <summary>
    /// Splits a full name into first and last name parts.
    /// </summary>
    /// <param name="fullName">Full display name.</param>
    /// <returns>First and last name.</returns>
    public static (string FirstName, string LastName) SplitFullName(string fullName)
    {
        var parts = fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            return ("User", "Account");
        }

        if (parts.Length == 1)
        {
            return (parts[0], parts[0]);
        }

        return (parts[0], string.Join(' ', parts.Skip(1)));
    }

    /// <summary>
    /// Normalizes an email for storage and lookup.
    /// </summary>
    /// <param name="email">Raw email.</param>
    /// <returns>Trimmed lowercase email.</returns>
    public static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    /// <summary>
    /// Loads the seeded role or throws.
    /// </summary>
    public static async Task<Role> GetRequiredRoleAsync(
        IApplicationDbContext dbContext,
        Guid roleId,
        string missingCode,
        CancellationToken cancellationToken)
    {
        return await dbContext.Roles.FirstOrDefaultAsync(r => r.Id == roleId, cancellationToken)
            ?? throw new BusinessRuleException("Required role is not seeded.", missingCode);
    }

    /// <summary>
    /// Builds an optional device upsert payload.
    /// </summary>
    public static DeviceUpsertRequest? ToDevice(
        string? deviceIdentifier,
        DevicePlatform? platform,
        string? fcmToken,
        string? appVersion,
        string? osVersion)
    {
        if (string.IsNullOrWhiteSpace(deviceIdentifier) || platform is null)
        {
            return null;
        }

        return new DeviceUpsertRequest(
            deviceIdentifier.Trim(),
            platform.Value,
            fcmToken,
            appVersion,
            osVersion);
    }
}
