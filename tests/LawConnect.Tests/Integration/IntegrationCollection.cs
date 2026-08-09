namespace LawConnect.Tests.Integration;

/// <summary>
/// Shares one web application factory across integration test classes to avoid
/// re-freezing the Serilog bootstrap logger.
/// </summary>
[CollectionDefinition("Integration")]
public sealed class IntegrationCollection : ICollectionFixture<LawConnectWebApplicationFactory>
{
}
