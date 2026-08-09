using System.Reflection;
using System.Threading.RateLimiting;
using Hangfire;
using LawConnect.Api.Realtime;
using LawConnect.Application.Common.Realtime;
using LawConnect.Shared.Constants;
using LawConnect.Shared.Options;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.FileProviders;
using Microsoft.OpenApi.Models;
using Serilog;

namespace LawConnect.Api.Extensions;

/// <summary>
/// API host service registration helpers.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers API controllers, Swagger, CORS, rate limiting, and health checks.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="configuration">Application configuration.</param>
    /// <param name="environment">Host environment.</param>
    /// <returns>The same service collection.</returns>
    public static IServiceCollection AddApiServices(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.AddOptions<CorsOptions>()
            .Bind(configuration.GetSection(CorsOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddControllers();
        services.AddEndpointsApiExplorer();
        services.AddSignalR();
        services.AddScoped<IChatRealtimePublisher, SignalRChatRealtimePublisher>();
        services.AddScoped<IEmergencyRealtimePublisher, SignalREmergencyRealtimePublisher>();
        AddRateLimiting(services, environment);
        AddSwagger(services);
        AddCors(services, configuration);
        AddHealthChecks(services, configuration, environment);

        return services;
    }

    private static void AddRateLimiting(IServiceCollection services, IHostEnvironment environment)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            if (environment.IsEnvironment("Testing"))
            {
                options.AddPolicy(AuthConstants.RateLimitPolicy, _ =>
                    RateLimitPartition.GetNoLimiter("testing"));
                options.AddPolicy("UploadEndpoints", _ =>
                    RateLimitPartition.GetNoLimiter("testing"));
                return;
            }

            options.AddPolicy(AuthConstants.RateLimitPolicy, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 20,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0
                    }));

            options.AddPolicy("UploadEndpoints", httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.User?.Identity?.Name
                                  ?? httpContext.Connection.RemoteIpAddress?.ToString()
                                  ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 10,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0
                    }));
        });
    }

    private static void AddHealthChecks(
        IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var healthChecks = services.AddHealthChecks();

        if (environment.IsEnvironment("Testing"))
        {
            return;
        }

        var defaultConnection = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");
        var redisConnection = configuration.GetConnectionString("Redis")
            ?? throw new InvalidOperationException("Connection string 'Redis' is not configured.");

        healthChecks
            .AddNpgSql(defaultConnection, name: "postgresql")
            .AddRedis(redisConnection, name: "redis")
            .AddHangfire(options =>
            {
                options.MinimumAvailableServers = 0;
            }, name: "hangfire");
    }

    private static void AddSwagger(IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "LawConnect API",
                Version = "v1",
                Description = "LawConnect marketplace API — auth, users, lawyers, appointments, and realtime chat."
            });

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "JWT Authorization header using the Bearer scheme."
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });

            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                options.IncludeXmlComments(xmlPath);
            }
        });
    }

    private static void AddCors(IServiceCollection services, IConfiguration configuration)
    {
        var corsOptions = configuration.GetSection(CorsOptions.SectionName).Get<CorsOptions>()
            ?? new CorsOptions();

        services.AddCors(options =>
        {
            options.AddPolicy(CorsPolicyNames.Default, policy =>
            {
                policy.WithOrigins(corsOptions.AllowedOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });
    }
}

/// <summary>
/// HTTP pipeline configuration helpers.
/// </summary>
public static class WebApplicationExtensions
{
    /// <summary>
    /// Configures middleware, auth, Hangfire dashboard, controllers, and health endpoints.
    /// </summary>
    /// <param name="app">Web application.</param>
    /// <returns>The same web application.</returns>
    public static WebApplication UseApiPipeline(this WebApplication app)
    {
        app.UseMiddleware<Middleware.CorrelationIdMiddleware>();
        app.UseMiddleware<Middleware.ExceptionHandlingMiddleware>();
        app.UseMiddleware<Middleware.RequestPerformanceMiddleware>();

        if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Testing"))
        {
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "LawConnect API v1");
                options.RoutePrefix = "swagger";
            });
        }

        app.UseSerilogRequestLogging(options =>
        {
            options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
            {
                diagnosticContext.Set("CorrelationId", httpContext.TraceIdentifier);
                diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
                diagnosticContext.Set("UserAgent", httpContext.Request.Headers.UserAgent.ToString());
            };
        });

        app.UseHttpsRedirection();
        app.UseCors(CorsPolicyNames.Default);
        app.UseRateLimiter();
        app.UseAuthentication();
        app.UseAuthorization();

        var storageOptions = app.Configuration.GetSection(StorageOptions.SectionName).Get<StorageOptions>()
            ?? new StorageOptions();
        var storageRoot = Path.IsPathRooted(storageOptions.RootPath)
            ? storageOptions.RootPath
            : Path.Combine(app.Environment.ContentRootPath, storageOptions.RootPath);
        Directory.CreateDirectory(storageRoot);
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(storageRoot),
            RequestPath = storageOptions.PublicBaseUrl.TrimEnd('/'),
            OnPrepareResponse = ctx =>
            {
                var path = ctx.Context.Request.Path.Value ?? string.Empty;
                var marker = storageOptions.PublicBaseUrl.TrimEnd('/');
                var relative = path;
                if (!string.IsNullOrEmpty(marker)
                    && path.StartsWith(marker, StringComparison.OrdinalIgnoreCase))
                {
                    relative = path[marker.Length..];
                }

                if (LawConnect.Infrastructure.Storage.StoragePath.IsPrivateKey(relative)
                    || path.Contains("/private/", StringComparison.OrdinalIgnoreCase))
                {
                    ctx.Context.Response.StatusCode = StatusCodes.Status404NotFound;
                    ctx.Context.Response.ContentLength = 0;
                    ctx.Context.Response.Headers.Remove("Content-Type");
                }
            }
        });

        if (!app.Environment.IsEnvironment("Testing"))
        {
            app.UseHangfireDashboard("/hangfire", new DashboardOptions
            {
                Authorization = [new Hangfire.Dashboard.LocalRequestsOnlyAuthorizationFilter()]
            });
            LawConnect.Infrastructure.Payments.OutboxHangfireRegistration.RegisterRecurringJobs();
        }

        if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Docker"))
        {
            using var scope = app.Services.CreateScope();
            var seeder = scope.ServiceProvider.GetRequiredService<LawConnect.Infrastructure.Persistence.Seeding.DemoDataSeeder>();
            seeder.SeedAsync().GetAwaiter().GetResult();
        }

        app.MapControllers();
        app.MapHub<Hubs.ChatHub>("/hubs/chat");
        app.MapHub<Hubs.EmergencyHub>("/hubs/emergency");
        app.MapHealthChecks("/health");

        return app;
    }
}
