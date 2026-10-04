namespace MakesCentsToMe.Integration.Infrastructure;

/// <summary>
/// Shares one factory (and therefore one PostgreSQL container) across every integration test class.
/// Test classes opt in with <c>[Collection("Integration")]</c>; they then run sequentially.
/// </summary>
[CollectionDefinition(Name)]
public class IntegrationTestCollection : ICollectionFixture<IntegrationTestWebApplicationFactory>
{
    public const string Name = "Integration";
}
