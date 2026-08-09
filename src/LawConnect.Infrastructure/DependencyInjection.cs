using System.Text;
using Hangfire;
using Hangfire.PostgreSql;
using LawConnect.Application.Common.Interfaces;
using LawConnect.Infrastructure.Geo;
using LawConnect.Infrastructure.Auth;
using LawConnect.Infrastructure.Events;
using LawConnect.Infrastructure.Identity;
using LawConnect.Infrastructure.Persistence;
using LawConnect.Infrastructure.Persistence.Interceptors;
using LawConnect.Infrastructure.Sms;
using LawConnect.Infrastructure.Storage;
using LawConnect.Shared.Constants;
using LawConnect.Shared.Infrastructure;
using LawConnect.Shared.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;

namespace LawConnect.Infrastructure;

/// <summary>
/// Infrastructure-layer dependency injection registration.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers persistence, caching, Hangfire, JWT authentication, and infrastructure services.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="configuration">Application configuration.</param>
    /// <param name="environment">Host environment.</param>
    /// <returns>The same service collection.</returns>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.AddOptions<ConnectionStringsOptions>()
            .Bind(configuration.GetSection(ConnectionStringsOptions.SectionName))
            .PostConfigure(options =>
            {
                if (string.IsNullOrWhiteSpace(options.HangfireConnection))
                {
                    options.HangfireConnection = options.DefaultConnection;
                }
            })
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<OtpOptions>()
            .Bind(configuration.GetSection(OtpOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<StorageOptions>()
            .Bind(configuration.GetSection(StorageOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<AwsS3Options>()
            .Bind(configuration.GetSection(AwsS3Options.SectionName));

        services.AddOptions<FirebaseOptions>()
            .Bind(configuration.GetSection(FirebaseOptions.SectionName));

        services.AddOptions<EmergencyOptions>()
            .Bind(configuration.GetSection(EmergencyOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<LawyerVerificationOptions>()
            .Bind(configuration.GetSection(LawyerVerificationOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<PaymentOptions>()
            .Bind(configuration.GetSection(PaymentOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddMemoryCache();
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUserService>();
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
        services.AddScoped<IIntegrationEventPublisher, IntegrationEventPublisher>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddSingleton<IOtpGenerator, OtpGenerator>();
        services.AddSingleton<ITokenHasher, TokenHasher>();
        services.AddSingleton<IPasswordHasher, AspNetPasswordHasher>();
        services.AddScoped<ISmsService, LoggingSmsService>();
        services.AddSingleton<IVirusScanner, PassThroughVirusScanner>();
        services.AddScoped<INotificationService, Notifications.FirebaseNotificationService>();
        services.AddScoped<IUserNotificationDispatcher, Notifications.UserNotificationDispatcher>();
        services.AddScoped<Persistence.Seeding.DemoDataSeeder>();
        RegisterPaymentProvider(services, configuration);
        RegisterFileStorage(services, configuration);
        services.AddScoped<AuditableEntityInterceptor>();
        services.AddScoped<DomainEventDispatchInterceptor>();

        AddPersistence(services, configuration, environment);
        AddCaching(services, configuration, environment);

        if (!environment.IsEnvironment("Testing"))
        {
            AddHangfire(services, configuration, environment);
            services.AddSingleton<IEmergencyTimeoutScheduler, Emergency.HangfireEmergencyTimeoutScheduler>();
            services.AddHttpClient<IReverseGeocoder, NominatimReverseGeocoder>(client =>
            {
                client.BaseAddress = new Uri("https://nominatim.openstreetmap.org/");
                client.DefaultRequestHeaders.UserAgent.ParseAdd("LawConnect/1.0 (emergency-sos)");
                client.Timeout = TimeSpan.FromSeconds(5);
            });
        }
        else
        {
            services.AddSingleton<IEmergencyTimeoutScheduler, Emergency.InlineEmergencyTimeoutScheduler>();
            services.AddSingleton<IReverseGeocoder, NullReverseGeocoder>();
        }

        AddJwtAuthentication(services, configuration, environment);

        return services;
    }

    private static void AddPersistence(
        IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        // Integration tests replace the provider with EF InMemory; avoid dual-provider registration.
        if (environment.IsEnvironment("Testing"))
        {
            return;
        }

        var connectionString = ResolvePostgresConnection(
            environment,
            "ConnectionStrings__DefaultConnection",
            configuration.GetConnectionString("DefaultConnection"),
            configuration["DATABASE_URL"]);

        services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
        {
            var auditableInterceptor = serviceProvider.GetRequiredService<AuditableEntityInterceptor>();
            var domainEventInterceptor = serviceProvider.GetRequiredService<DomainEventDispatchInterceptor>();

            options.UseNpgsql(connectionString, npgsql =>
            {
                npgsql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
                npgsql.EnableRetryOnFailure(3);
            });

            options.AddInterceptors(auditableInterceptor, domainEventInterceptor);
        });

        services.AddScoped<IApplicationDbContext>(provider =>
            provider.GetRequiredService<ApplicationDbContext>());
    }

    private static void AddCaching(
        IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        if (environment.IsEnvironment("Testing"))
        {
            services.AddDistributedMemoryCache();
            return;
        }

        var redisConnection = ConnectionStringResolver.Coalesce(
            configuration.GetConnectionString("Redis"),
            configuration["REDIS_URL"])
            ?? throw new InvalidOperationException(
                $"Connection string '{nameof(ConnectionStringsOptions.Redis)}' is not configured.");
        ConnectionStringResolver.EnsureNotLoopbackInProduction(
            "ConnectionStrings__Redis",
            redisConnection,
            environment.IsProduction());
        redisConnection = RedisConnectionString.Normalize(redisConnection);

        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConnection;
            options.InstanceName = "LawConnect:";
        });
    }

    private static void AddHangfire(
        IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var hangfireConnection = ResolvePostgresConnection(
            environment,
            "ConnectionStrings__DefaultConnection",
            configuration.GetConnectionString("HangfireConnection"),
            configuration.GetConnectionString("DefaultConnection"),
            configuration["DATABASE_URL"]);

        services.AddHangfire(config =>
        {
            config.SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UsePostgreSqlStorage(options =>
                {
                    options.UseNpgsqlConnection(hangfireConnection);
                });
        });

        services.AddHangfireServer();
    }

    private static void AddJwtAuthentication(
        IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var jwtOptions = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
            ?? throw new InvalidOperationException($"Configuration section '{JwtOptions.SectionName}' is missing.");

        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = !(environment.IsDevelopment() || environment.IsEnvironment("Testing") || environment.IsEnvironment("Docker"));
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidAudience = jwtOptions.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Secret)),
                    ClockSkew = TimeSpan.FromMinutes(1)
                };
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;
                        if (!string.IsNullOrEmpty(accessToken)
                            && (path.StartsWithSegments("/hubs/chat") || path.StartsWithSegments("/hubs/emergency")))
                        {
                            context.Token = accessToken;
                        }

                        return Task.CompletedTask;
                    }
                };
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy(PolicyNames.CustomerOnly, policy =>
                policy.RequireRole(Roles.Customer));

            options.AddPolicy(PolicyNames.LawyerOnly, policy =>
                policy.RequireRole(Roles.Lawyer));

            options.AddPolicy(PolicyNames.AdminOnly, policy =>
                policy.RequireRole(Roles.Admin));

            options.AddPolicy(PolicyNames.CustomerOrLawyer, policy =>
                policy.RequireRole(Roles.Customer, Roles.Lawyer));
        });
    }

    private static void RegisterPaymentProvider(IServiceCollection services, IConfiguration configuration)
    {
        var paymentOptions = configuration.GetSection(PaymentOptions.SectionName).Get<PaymentOptions>()
                             ?? new PaymentOptions();

        if (string.Equals(paymentOptions.Provider, "Razorpay", StringComparison.OrdinalIgnoreCase))
        {
            services.AddHttpClient<Payments.RazorpayPaymentProvider>();
            services.AddScoped<IPaymentProvider>(sp => sp.GetRequiredService<Payments.RazorpayPaymentProvider>());
            services.AddScoped<IPaymentGateway>(sp => sp.GetRequiredService<Payments.RazorpayPaymentProvider>());
        }
        else
        {
            services.AddSingleton<Payments.FakePaymentProvider>();
            services.AddSingleton<IPaymentProvider>(sp => sp.GetRequiredService<Payments.FakePaymentProvider>());
            services.AddSingleton<IPaymentGateway>(sp => sp.GetRequiredService<Payments.FakePaymentProvider>());
        }
    }

    private static void RegisterFileStorage(IServiceCollection services, IConfiguration configuration)
    {
        var s3 = configuration.GetSection(AwsS3Options.SectionName).Get<AwsS3Options>() ?? new AwsS3Options();
        if (s3.Enabled)
        {
            services.AddSingleton<IFileStorage, S3FileStorage>();
        }
        else
        {
            services.AddSingleton<IFileStorage, LocalFileStorage>();
        }
    }

    private static string ResolvePostgresConnection(
        IHostEnvironment environment,
        string environmentVariableName,
        params string?[] candidates)
    {
        var raw = ConnectionStringResolver.Coalesce(candidates)
            ?? throw new InvalidOperationException(
                $"{environmentVariableName} is not configured.");
        ConnectionStringResolver.EnsureNotLoopbackInProduction(
            environmentVariableName,
            raw,
            environment.IsProduction());
        return PostgresConnectionString.Normalize(raw);
    }
}
