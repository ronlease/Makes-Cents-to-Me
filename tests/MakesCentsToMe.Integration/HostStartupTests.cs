using FluentAssertions;
using MakesCentsToMe.Api.Features.Categories;
using MakesCentsToMe.Integration.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace MakesCentsToMe.Integration;

// Feature: Integration test host
//
// Scenario: Migrations are applied to the PostgreSQL container
//   Given the API host has started against a fresh PostgreSQL container
//   When the applied migrations are compared with the migrations in the assembly
//   Then every migration is applied and none is pending
//
// Scenario: Default categories are seeded
//   Given the API host has started
//   When the categories are listed
//   Then the 17 seeded default categories are returned
//
// Scenario: Claude analysis is routed through the stub
//   Given an account with an import profile
//   When one row is imported
//   Then the stub receives a request addressed to the Anthropic messages endpoint
[Collection(IntegrationTestCollection.Name)]
public class HostStartupTests(IntegrationTestWebApplicationFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task ClaudeClient_ImportingOneRow_IsRoutedThroughStub()
    {
        // Arrange
        var accountId = await ApiSeeder.CreateAccountWithProfileAsync(Client);
        var csv = string.Join("\n", CsvSamples.Header, CsvSamples.ShellLine) + "\n";

        // Act
        await ApiSeeder.ProcessImportAsync(Client, accountId, csv);

        // Assert
        ClaudeRequestRecorder.Count.Should().Be(1);
        ClaudeRequestRecorder.Requests[0].RequestUri.Should().Be(new Uri("https://api.anthropic.com/v1/messages"));
        ClaudeRequestRecorder.Descriptions.Should().Equal("SHELL OIL 5551");
    }

    [Fact]
    public async Task DefaultCategories_ListedThroughApi_ReturnsSeventeenSeededCategories()
    {
        // Arrange
        // (the migrations seed the default categories)

        // Act
        var response = await Client.GetAsync("/api/v1/categories");
        var body = await ApiJson.ReadApiResponseAsync<List<CategoryResponse>>(response);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        body.Data.Should().HaveCount(17);
        body.Data.Should().OnlyContain(category => category.IsDefault);
    }

    [Fact]
    public async Task Migrations_AfterHostStartup_AreAllApplied()
    {
        // Arrange
        await using var scope = CreateDbContextScope();

        // Act
        var applied = await scope.DbContext.Database.GetAppliedMigrationsAsync();
        var pending = await scope.DbContext.Database.GetPendingMigrationsAsync();

        // Assert
        applied.Should().Equal(scope.DbContext.Database.GetMigrations());
        pending.Should().BeEmpty();
    }
}
