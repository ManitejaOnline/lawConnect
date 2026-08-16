using LawConnect.Application.Common.Interfaces;
using LawConnect.Domain.Entities;
using LawConnect.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LawConnect.Infrastructure.Persistence.Seeding;

/// <summary>
/// Bootstraps the initial platform administrator account from <c>ADMIN_USERNAME</c> /
/// <c>ADMIN_PASSWORD</c> configuration. Idempotent: never overwrites an existing admin's
/// password on subsequent restarts.
/// </summary>
public sealed class AdminAccountSeeder
{
    private readonly ApplicationDbContext _db;
    private readonly IConfiguration _configuration;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<AdminAccountSeeder> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AdminAccountSeeder"/> class.
    /// </summary>
    public AdminAccountSeeder(
        ApplicationDbContext db,
        IConfiguration configuration,
        IPasswordHasher passwordHasher,
        ILogger<AdminAccountSeeder> logger)
    {
        _db = db;
        _configuration = configuration;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    /// <summary>
    /// Creates the initial admin account if configured and not already present.
    /// </summary>
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var username = _configuration["ADMIN_USERNAME"];
        var password = _configuration["ADMIN_PASSWORD"];

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            _logger.LogWarning(
                "ADMIN_USERNAME/ADMIN_PASSWORD not configured — skipping admin account bootstrap");
            return;
        }

        var email = username.Trim().ToLowerInvariant();
        var exists = await _db.Users.AnyAsync(u => u.Email == email, cancellationToken);
        if (exists)
        {
            _logger.LogInformation("Admin account already exists — skipping bootstrap");
            return;
        }

        var user = new User
        {
            Email = email,
            PhoneNumber = $"+000{Guid.NewGuid():N}"[..15],
            PasswordHash = _passwordHasher.Hash(password),
            FirstName = "Admin",
            LastName = string.Empty,
            IsActive = true,
            IsEmailVerified = true
        };
        user.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = SeedIds.Roles.Admin });

        _db.Users.Add(user);
        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Seeded initial admin account for {Email}", email);
    }
}
