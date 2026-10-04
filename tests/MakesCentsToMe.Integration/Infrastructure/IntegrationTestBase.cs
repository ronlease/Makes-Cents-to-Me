using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MakesCentsToMe.Integration.Infrastructure;

/// <summary>
/// Base class for integration tests. Resets the database and the Claude recorder before every test.
/// Derive and decorate with <c>[Collection("Integration")]</c>.
/// </summary>
public abstract class IntegrationTestBase(IntegrationTestWebApplicationFactory factory) : IAsyncLifetime
{
    public HttpClient Client { get; } = factory.CreateApiClient();

    public ClaudeRequestRecorder ClaudeRequestRecorder { get; } =
        factory.Services.GetRequiredService<ClaudeRequestRecorder>();

    public IntegrationTestWebApplicationFactory Factory { get; } = factory;

    public DatabaseScope CreateDbContextScope() => new(Factory.Services.CreateAsyncScope());

    public virtual Task DisposeAsync()
    {
        Client.Dispose();
        return Task.CompletedTask;
    }

    public virtual async Task InitializeAsync() => await ResetDatabaseAsync();

    public async Task ResetDatabaseAsync()
    {
        await using var scope = CreateDbContextScope();
        await scope.DbContext.Database.ExecuteSqlRawAsync(
            """TRUNCATE "Transactions", "LearnedRules", "ColumnMappings", "ImportProfiles", "Accounts", "Institutions" RESTART IDENTITY CASCADE;""");
        await scope.DbContext.Database.ExecuteSqlRawAsync(
            """DELETE FROM "Categories" WHERE "IsDefault" = false;""");
        ClaudeRequestRecorder.Clear();
    }
}
