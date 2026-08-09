using LawConnect.Application;
using LawConnect.Api.Configuration;
using LawConnect.Api.Extensions;
using LawConnect.Infrastructure;
using LawConnect.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

try
{
    Log.Information("Starting LawConnect API");

    var builder = WebApplication.CreateBuilder(args);
    builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);
    builder.Configuration.AddEmergencyEnvironmentOverrides();
    builder.Configuration.AddProductionConnectionFallbacks();

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithEnvironmentName()
        .Enrich.WithMachineName()
        .Enrich.WithThreadId());

    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration, builder.Environment);
    builder.Services.AddApiServices(builder.Configuration, builder.Environment);

    var app = builder.Build();

    var applyMigrations = app.Configuration.GetValue<bool?>("Database:ApplyMigrations")
        ?? app.Environment.IsProduction();
    if (applyMigrations && !app.Environment.IsEnvironment("Testing"))
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.MigrateAsync();
    }

    app.UseApiPipeline();
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "LawConnect API terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

/// <summary>
/// Application entry point marker used by integration tests.
/// </summary>
public partial class Program;
