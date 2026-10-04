using FluentAssertions;
using MakesCentsToMe.Api.Features.LearnedRules;
using MakesCentsToMe.Api.Models.Entities;
using MakesCentsToMe.Integration.Infrastructure;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Http.Json;

namespace MakesCentsToMe.Integration.Features.LearnedRules;

// Feature: Learned rule endpoint contracts (/api/v1/learned-rules)
//
// Scenario: List and get learned rules
//   Given learned rules exist
//   When the list or a single rule is requested
//   Then 200 is returned (list ordered by pattern) and an unknown id returns 404
//
// Scenario: Create a learned rule
//   Given an existing category
//   When a rule is posted
//   Then 201 is returned with a Location header and the pattern is normalized
//   And a blank or too-short pattern, blank vendor, unknown category, or duplicate pattern returns 400
//
// Scenario: Update a learned rule
//   Given an existing rule
//   When it is updated
//   Then 200 is returned, an unknown id returns 404, and validation failures return 400
//
// Scenario: Delete a learned rule
//   Given a rule that categorized transactions
//   When it is deleted
//   Then 204 with an empty body is returned and the transactions keep their category and vendor but lose the rule link
//   And an unknown id returns 404
[Collection(IntegrationTestCollection.Name)]
public class LearnedRuleEndpointContractTests(IntegrationTestWebApplicationFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task Create_BlankNormalizedVendor_ReturnsBadRequest()
    {
        // Arrange
        var groceriesId = await ApiSeeder.GetCategoryIdAsync(Client, "Groceries");

        // Act
        var response = await PostRuleAsync(new CreateLearnedRuleRequest(groceriesId, "  ", "WHOLE FOODS", null));
        var body = await ApiJson.ReadApiResponseAsync<LearnedRuleResponse>(response);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        body.Errors.Should().ContainSingle().Which.Should().Contain("Normalized vendor is required");
    }

    [Fact]
    public async Task Create_BlankPattern_ReturnsBadRequest()
    {
        // Arrange
        var groceriesId = await ApiSeeder.GetCategoryIdAsync(Client, "Groceries");

        // Act
        var response = await PostRuleAsync(new CreateLearnedRuleRequest(groceriesId, "Whole Foods", "   ", null));
        var body = await ApiJson.ReadApiResponseAsync<LearnedRuleResponse>(response);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        body.Success.Should().BeFalse();
        body.Errors.Should().ContainSingle().Which.Should().Contain("Pattern is required");
    }

    [Fact]
    public async Task Create_DuplicatePattern_ReturnsBadRequest()
    {
        // Arrange
        var groceriesId = await ApiSeeder.GetCategoryIdAsync(Client, "Groceries");
        await ApiSeeder.CreateLearnedRuleAsync(Client, groceriesId, "Whole Foods", "WHOLE FOODS");

        // Act
        var response = await PostRuleAsync(new CreateLearnedRuleRequest(groceriesId, "Other", "whole  foods", null));
        var body = await ApiJson.ReadApiResponseAsync<LearnedRuleResponse>(response);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        body.Errors.Should().ContainSingle().Which.Should().Contain("already exists");
    }

    [Fact]
    public async Task Create_PatternShorterThanThreeCharacters_ReturnsBadRequest()
    {
        // Arrange
        var groceriesId = await ApiSeeder.GetCategoryIdAsync(Client, "Groceries");

        // Act
        var response = await PostRuleAsync(new CreateLearnedRuleRequest(groceriesId, "Ab", "ab", null));
        var body = await ApiJson.ReadApiResponseAsync<LearnedRuleResponse>(response);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        body.Errors.Should().ContainSingle().Which.Should().Contain("at least 3 characters");
    }

    [Fact]
    public async Task Create_UnknownCategory_ReturnsBadRequest()
    {
        // Arrange
        var unknownCategoryId = Guid.NewGuid();

        // Act
        var response = await PostRuleAsync(new CreateLearnedRuleRequest(unknownCategoryId, "Whole Foods", "WHOLE FOODS", null));
        var body = await ApiJson.ReadApiResponseAsync<LearnedRuleResponse>(response);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        body.Errors.Should().ContainSingle().Which.Should().Contain("not found");
    }

    [Fact]
    public async Task Create_Valid_ReturnsCreatedWithLocationAndNormalizedPattern()
    {
        // Arrange
        var groceriesId = await ApiSeeder.GetCategoryIdAsync(Client, "Groceries");

        // Act
        var response = await PostRuleAsync(new CreateLearnedRuleRequest(groceriesId, "  Whole Foods ", "  whole   foods ", null));
        using var document = await ApiJson.ReadJsonDocumentAsync(response);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        ApiJson.AssertApiResponseEnvelope(document);
        var data = document.RootElement.GetProperty("data");
        var id = data.GetProperty("id").GetGuid();
        response.Headers.Location!.ToString().Should().Be($"/api/v1/learned-rules/{id}");
        data.GetProperty("pattern").GetString().Should().Be("WHOLE FOODS");
        data.GetProperty("normalizedVendor").GetString().Should().Be("Whole Foods");
        data.GetProperty("categoryId").GetGuid().Should().Be(groceriesId);
        data.GetProperty("categoryName").GetString().Should().Be("Groceries");
        data.TryGetProperty("createdAt", out _).Should().BeTrue();
        data.TryGetProperty("updatedAt", out _).Should().BeTrue();
    }

    [Fact]
    public async Task Delete_ExistingRuleWithCategorizedTransactions_ReturnsNoContentAndDetachesRule()
    {
        // Arrange
        var groceriesId = await ApiSeeder.GetCategoryIdAsync(Client, "Groceries");
        var rule = await ApiSeeder.CreateLearnedRuleAsync(Client, groceriesId, "Shell", "SHELL OIL");
        var accountId = await ApiSeeder.CreateAccountWithProfileAsync(Client);
        await ApiSeeder.ProcessImportAsync(Client, accountId, CsvSamples.FirstImport);

        // Act
        var response = await Client.DeleteAsync($"/api/v1/learned-rules/{rule.Id}");
        var body = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        body.Should().BeEmpty();
        await using var scope = CreateDbContextScope();
        var transaction = await scope.DbContext.Transactions.AsNoTracking()
            .SingleAsync(t => t.Description == "SHELL OIL 5551");
        transaction.LearnedRuleId.Should().BeNull();
        transaction.CategoryId.Should().Be(groceriesId);
        transaction.NormalizedVendor.Should().Be("Shell");
        transaction.Status.Should().Be(TransactionStatus.Committed);
        (await scope.DbContext.LearnedRules.AnyAsync()).Should().BeFalse();
    }

    [Fact]
    public async Task Delete_MissingRule_ReturnsNotFound()
    {
        // Arrange
        var unknownId = Guid.NewGuid();

        // Act
        var response = await Client.DeleteAsync($"/api/v1/learned-rules/{unknownId}");
        var body = await ApiJson.ReadApiResponseAsync<bool>(response);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        body.Success.Should().BeFalse();
        body.Errors.Should().ContainSingle();
    }

    [Fact]
    public async Task GetById_Existing_ReturnsOk()
    {
        // Arrange
        var groceriesId = await ApiSeeder.GetCategoryIdAsync(Client, "Groceries");
        var rule = await ApiSeeder.CreateLearnedRuleAsync(Client, groceriesId, "Whole Foods", "WHOLE FOODS");

        // Act
        var response = await Client.GetAsync($"/api/v1/learned-rules/{rule.Id}");
        var body = await ApiJson.ReadApiResponseAsync<LearnedRuleResponse>(response);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body.Data.Should().NotBeNull();
        body.Data!.Id.Should().Be(rule.Id);
        body.Data.Pattern.Should().Be("WHOLE FOODS");
        body.Data.NormalizedVendor.Should().Be("Whole Foods");
        body.Data.CategoryName.Should().Be("Groceries");
    }

    [Fact]
    public async Task GetById_Missing_ReturnsNotFound()
    {
        // Arrange
        var unknownId = Guid.NewGuid();

        // Act
        var response = await Client.GetAsync($"/api/v1/learned-rules/{unknownId}");
        var body = await ApiJson.ReadApiResponseAsync<LearnedRuleResponse>(response);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        body.Success.Should().BeFalse();
        body.Errors.Should().ContainSingle().Which.Should().Contain("not found");
    }

    [Fact]
    public async Task List_RulesExist_ReturnsOkOrderedByPattern()
    {
        // Arrange
        var groceriesId = await ApiSeeder.GetCategoryIdAsync(Client, "Groceries");
        await ApiSeeder.CreateLearnedRuleAsync(Client, groceriesId, "Zeta", "ZETA MARKET");
        await ApiSeeder.CreateLearnedRuleAsync(Client, groceriesId, "Alpha", "ALPHA MARKET");

        // Act
        var response = await Client.GetAsync("/api/v1/learned-rules");
        var body = await ApiJson.ReadApiResponseAsync<List<LearnedRuleResponse>>(response);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body.Data!.Select(rule => rule.Pattern).Should().Equal("ALPHA MARKET", "ZETA MARKET");
    }

    [Fact]
    public async Task List_NoRules_ReturnsOkWithEmptyList()
    {
        // Arrange
        // (database was reset before the test)

        // Act
        var response = await Client.GetAsync("/api/v1/learned-rules");
        using var document = await ApiJson.ReadJsonDocumentAsync(response);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        ApiJson.AssertApiResponseEnvelope(document);
        document.RootElement.GetProperty("data").GetArrayLength().Should().Be(0);
        document.RootElement.GetProperty("success").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task Update_Existing_ReturnsOkWithChangedValues()
    {
        // Arrange
        var groceriesId = await ApiSeeder.GetCategoryIdAsync(Client, "Groceries");
        var shoppingId = await ApiSeeder.GetCategoryIdAsync(Client, "Shopping");
        var rule = await ApiSeeder.CreateLearnedRuleAsync(Client, groceriesId, "Whole Foods", "WHOLE FOODS");

        // Act
        var response = await Client.PutAsJsonAsync(
            $"/api/v1/learned-rules/{rule.Id}",
            new UpdateLearnedRuleRequest(shoppingId, "Target", "target store"),
            ApiJson.Options);
        var body = await ApiJson.ReadApiResponseAsync<LearnedRuleResponse>(response);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body.Data!.Id.Should().Be(rule.Id);
        body.Data.Pattern.Should().Be("TARGET STORE");
        body.Data.NormalizedVendor.Should().Be("Target");
        body.Data.CategoryId.Should().Be(shoppingId);
        body.Data.CategoryName.Should().Be("Shopping");
        body.Data.UpdatedAt.Should().BeOnOrAfter(rule.UpdatedAt);
    }

    [Fact]
    public async Task Update_Missing_ReturnsNotFound()
    {
        // Arrange
        var groceriesId = await ApiSeeder.GetCategoryIdAsync(Client, "Groceries");

        // Act
        var response = await Client.PutAsJsonAsync(
            $"/api/v1/learned-rules/{Guid.NewGuid()}",
            new UpdateLearnedRuleRequest(groceriesId, "Whole Foods", "WHOLE FOODS"),
            ApiJson.Options);
        var body = await ApiJson.ReadApiResponseAsync<LearnedRuleResponse>(response);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        body.Success.Should().BeFalse();
    }

    [Fact]
    public async Task Update_PatternDuplicatesAnotherRule_ReturnsBadRequest()
    {
        // Arrange
        var groceriesId = await ApiSeeder.GetCategoryIdAsync(Client, "Groceries");
        await ApiSeeder.CreateLearnedRuleAsync(Client, groceriesId, "Alpha", "ALPHA MARKET");
        var rule = await ApiSeeder.CreateLearnedRuleAsync(Client, groceriesId, "Zeta", "ZETA MARKET");

        // Act
        var response = await Client.PutAsJsonAsync(
            $"/api/v1/learned-rules/{rule.Id}",
            new UpdateLearnedRuleRequest(groceriesId, "Zeta", "alpha market"),
            ApiJson.Options);
        var body = await ApiJson.ReadApiResponseAsync<LearnedRuleResponse>(response);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        body.Errors.Should().ContainSingle().Which.Should().Contain("already exists");
    }

    private async Task<HttpResponseMessage> PostRuleAsync(CreateLearnedRuleRequest request) =>
        await Client.PostAsJsonAsync("/api/v1/learned-rules", request, ApiJson.Options);
}
