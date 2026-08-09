namespace LawConnect.Shared.Constants;

public static class ConfigurationKeys
{
    public const string DefaultConnection = "ConnectionStrings:DefaultConnection";
    public const string RedisConnection = "ConnectionStrings:Redis";
    public const string HangfireConnection = "ConnectionStrings:HangfireConnection";

    public const string JwtSection = "Jwt";
    public const string CorsSection = "Cors";
    public const string SerilogSection = "Serilog";
}
