using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using LawConnect.Application.Common.Interfaces;
using LawConnect.Domain.Entities;
using LawConnect.Domain.Enums;
using LawConnect.Infrastructure.Persistence;
using LawConnect.Infrastructure.Persistence.Interceptors;
using LawConnect.Infrastructure.Persistence.Seeding;
using LawConnect.Shared.Constants;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LawConnect.Tests.Integration;

/// <summary>
/// Shared web application factory for API integration tests.
/// </summary>
public sealed class LawConnectWebApplicationFactory : WebApplicationFactory<Program>
{
    public CapturingSmsService Sms { get; } = new();

    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<ISmsService>();

            var databaseName = $"LawConnectTests-{Guid.NewGuid()}";
            services.AddDbContext<ApplicationDbContext>((sp, options) =>
            {
                options.UseInMemoryDatabase(databaseName);
                options.AddInterceptors(
                    sp.GetRequiredService<AuditableEntityInterceptor>(),
                    sp.GetRequiredService<DomainEventDispatchInterceptor>());
            });
            services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
            services.AddSingleton<ISmsService>(Sms);
        });
    }

    /// <inheritdoc />
    protected override void ConfigureClient(HttpClient client)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Database.EnsureCreated();
        SeedReferenceData(db);
        base.ConfigureClient(client);
    }

    /// <summary>
    /// Marks a lawyer as approved and operational for tests that exercise marketplace features.
    /// </summary>
    public async Task ApproveLawyerAsync(Guid lawyerId)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var lawyer = await db.Lawyers.FirstAsync(l => l.Id == lawyerId);
        lawyer.VerificationStatus = VerificationStatus.Approved;
        lawyer.IsVerified = true;
        lawyer.IsAcceptingAppointments = true;
        lawyer.VerifiedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Authenticates via OTP and returns a bearer-authorized client plus login payload.
    /// </summary>
    public async Task<(HttpClient Client, LoginDto Login)> CreateAuthenticatedClientAsync(string phoneNumber)
    {
        var client = CreateClient();
        Sms.Reset();

        var requestResponse = await client.PostAsJsonAsync(
            "/api/auth/otp/request",
            new { phoneNumber });
        requestResponse.EnsureSuccessStatusCode();

        var otp = Sms.LastMessage is null
            ? throw new InvalidOperationException("OTP SMS was not captured.")
            : Regex.Match(Sms.LastMessage, @"\b(\d{6})\b").Groups[1].Value;

        var verifyResponse = await client.PostAsJsonAsync(
            "/api/auth/otp/verify",
            new
            {
                phoneNumber,
                code = otp,
                deviceIdentifier = "test-device-1",
                platform = DevicePlatform.Android
            });
        verifyResponse.EnsureSuccessStatusCode();

        var login = await verifyResponse.Content.ReadFromJsonAsync<LoginDto>(JsonOptions)
            ?? throw new InvalidOperationException("Login payload missing.");

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", login.Tokens.AccessToken);

        return (client, login);
    }

    internal static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static void SeedReferenceData(ApplicationDbContext db)
    {
        if (!db.Roles.Any())
        {
            db.Roles.AddRange(
                new Role
                {
                    Id = SeedIds.Roles.Customer,
                    Name = Roles.Customer,
                    Description = "Customer",
                    CreatedAt = DateTime.UtcNow
                },
                new Role
                {
                    Id = SeedIds.Roles.Lawyer,
                    Name = Roles.Lawyer,
                    Description = "Lawyer",
                    CreatedAt = DateTime.UtcNow
                },
                new Role
                {
                    Id = SeedIds.Roles.Admin,
                    Name = Roles.Admin,
                    Description = "Admin",
                    CreatedAt = DateTime.UtcNow
                });
        }

        if (!db.Specializations.Any())
        {
            db.Specializations.AddRange(
                new Specialization
                {
                    Id = SeedIds.Specializations.CriminalLaw,
                    Name = "Criminal Law",
                    Slug = "criminal-law",
                    IsActive = true,
                    DisplayOrder = 1,
                    CreatedAt = DateTime.UtcNow
                },
                new Specialization
                {
                    Id = SeedIds.Specializations.CivilLaw,
                    Name = "Civil Law",
                    Slug = "civil-law",
                    IsActive = true,
                    DisplayOrder = 2,
                    CreatedAt = DateTime.UtcNow
                },
                new Specialization
                {
                    Id = SeedIds.Specializations.FamilyLaw,
                    Name = "Family Law",
                    Slug = "family-law",
                    IsActive = true,
                    DisplayOrder = 3,
                    CreatedAt = DateTime.UtcNow
                });
        }

        if (!db.EmergencyCategories.Any())
        {
            var seedAt = DateTime.UtcNow;
            db.EmergencyCategories.AddRange(
                new Domain.Entities.EmergencyCategory
                {
                    Id = SeedIds.EmergencyCategories.CriminalEmergency,
                    Name = "Criminal Emergency",
                    Slug = "criminal-emergency",
                    SpecializationId = SeedIds.Specializations.CriminalLaw,
                    IsActive = true,
                    DisplayOrder = 1,
                    CreatedAt = seedAt
                },
                new Domain.Entities.EmergencyCategory
                {
                    Id = SeedIds.EmergencyCategories.CivilEmergency,
                    Name = "Civil Emergency",
                    Slug = "civil-emergency",
                    SpecializationId = SeedIds.Specializations.CivilLaw,
                    IsActive = true,
                    DisplayOrder = 2,
                    CreatedAt = seedAt
                },
                new Domain.Entities.EmergencyCategory
                {
                    Id = SeedIds.EmergencyCategories.FamilyCrisis,
                    Name = "Family Crisis",
                    Slug = "family-crisis",
                    SpecializationId = SeedIds.Specializations.FamilyLaw,
                    IsActive = true,
                    DisplayOrder = 3,
                    CreatedAt = seedAt
                },
                new Domain.Entities.EmergencyCategory
                {
                    Id = SeedIds.EmergencyCategories.CyberCrime,
                    Name = "Cyber Crime",
                    Slug = "cyber-crime",
                    SpecializationId = SeedIds.Specializations.CyberLaw,
                    IsActive = true,
                    DisplayOrder = 4,
                    CreatedAt = seedAt
                },
                new Domain.Entities.EmergencyCategory
                {
                    Id = SeedIds.EmergencyCategories.ConsumerEmergency,
                    Name = "Consumer Emergency",
                    Slug = "consumer-emergency",
                    SpecializationId = SeedIds.Specializations.ConsumerProtection,
                    IsActive = true,
                    DisplayOrder = 5,
                    CreatedAt = seedAt
                });
        }

        db.SaveChanges();
        GeographySeeder.Ensure(db);
    }

    /// <summary>Cancels open SOS and takes all lawyers offline so matching tests stay isolated.</summary>
    public async Task ResetEmergencyStateAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        foreach (var sos in await db.EmergencyRequests.ToListAsync())
        {
            if (EmergencyLifecycle.IsOpen(sos.Status) || EmergencyLifecycle.IsAssignedLike(sos.Status))
            {
                sos.Status = EmergencyStatus.Cancelled;
                sos.CancellationReason = "test reset";
            }
        }

        foreach (var assignment in await db.EmergencyAssignments.ToListAsync())
        {
            if (assignment.Status is EmergencyAssignmentStatus.Pending or EmergencyAssignmentStatus.Accepted)
            {
                assignment.Status = EmergencyAssignmentStatus.Cancelled;
            }
        }

        foreach (var lawyer in await db.Lawyers.ToListAsync())
        {
            lawyer.IsOnline = false;
            lawyer.IsAvailableForEmergency = false;
        }

        await db.SaveChangesAsync();
    }
}

/// <summary>
/// Captures outbound SMS for OTP tests.
/// </summary>
public sealed class CapturingSmsService : ISmsService
{
    public string? LastPhoneNumber { get; private set; }
    public string? LastMessage { get; private set; }

    /// <summary>
    /// Clears captured messages.
    /// </summary>
    public void Reset()
    {
        LastPhoneNumber = null;
        LastMessage = null;
    }

    /// <inheritdoc />
    public Task SendAsync(string phoneNumber, string message, CancellationToken cancellationToken = default)
    {
        LastPhoneNumber = phoneNumber;
        LastMessage = message;
        return Task.CompletedTask;
    }
}

/// <summary>
/// Login DTO for integration tests.
/// </summary>
public sealed record LoginDto(Guid UserId, string PhoneNumber, string[] Roles, bool IsNewUser, TokenDto Tokens);

/// <summary>
/// Token DTO for integration tests.
/// </summary>
public sealed record TokenDto(
    string AccessToken,
    DateTime AccessTokenExpiresAtUtc,
    string RefreshToken,
    DateTime RefreshTokenExpiresAtUtc);
