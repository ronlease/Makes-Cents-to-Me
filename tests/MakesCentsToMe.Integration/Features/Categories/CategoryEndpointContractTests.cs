using FluentAssertions;
using MakesCentsToMe.Api.Features.Categories;
using MakesCentsToMe.Api.Models.Entities;
using MakesCentsToMe.Integration.Infrastructure;
using System.Net;
using System.Net.Http.Json;

namespace MakesCentsToMe.Integration.Features.Categories;

// Feature: Category endpoint contract
//
// Scenario: Creating a category returns 201 with a Location header
//   When a valid category is posted
//   Then the response is 201 Created with an ApiResponse envelope, the category fields, and a Location header
//
// Scenario: Creating a category with a blank name is rejected with 400
//
// Scenario: Creating a category whose name duplicates a custom or seeded default category is rejected with 400
//
// Scenario: Listing categories returns the 17 seeded defaults ordered by name
//
// Scenario: Listing categories reports each category's transaction count
//
// Scenario: Getting a category returns 200; a missing category returns 404
//
// Scenario: Updating a category returns 200 with the new name
//
// Scenario: Updating with a blank name returns 400; updating a missing category returns 404
//
// Scenario: Updating to a duplicate name returns 404 (current behavior, risk R5)
//
// Scenario: Deleting an unused custom category returns 204 with an empty body
//
// Scenario: Deleting a category that has transactions, or is used by a learned rule, is rejected with 400
//
// Scenario: Deleting a missing category returns 400 (current behavior, risk R5)
[Collection(IntegrationTestCollection.Name)]
public class CategoryEndpointContractTests(IntegrationTestWebApplicationFactory factory)
    : IntegrationTestBase(factory)
{
    private const string CategoriesUrl = "/api/v1/categories";

    [Fact]
    public async Task Create_BlankName_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateCategoryRequest("  ");

        // Act
        var response = await Client.PostAsJsonAsync(CategoriesUrl, request, ApiJson.Options);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await ApiJson.ReadApiResponseAsync<CategoryResponse>(response);
        body.Success.Should().BeFalse();
        body.Errors.Should().Contain("Name is required.");
    }

    [Fact]
    public async Task Create_DuplicateOfCustomCategory_ReturnsBadRequest()
    {
        // Arrange
        await ApiSeeder.CreateCategoryAsync(Client, "Pet Care");

        // Act
        var response = await Client.PostAsJsonAsync(
            CategoriesUrl,
            new CreateCategoryRequest("Pet Care"),
            ApiJson.Options);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await ApiJson.ReadApiResponseAsync<CategoryResponse>(response);
        body.Success.Should().BeFalse();
        body.Errors.Should().ContainSingle().Which.Should().Contain("already exists");
    }

    [Fact]
    public async Task Create_DuplicateOfSeededDefaultCategory_ReturnsBadRequest()
    {
        // Arrange
        var defaultName = await GetFirstDefaultCategoryNameAsync();

        // Act
        var response = await Client.PostAsJsonAsync(
            CategoriesUrl,
            new CreateCategoryRequest(defaultName),
            ApiJson.Options);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await ApiJson.ReadApiResponseAsync<CategoryResponse>(response);
        body.Success.Should().BeFalse();
        body.Errors.Should().ContainSingle().Which.Should().Contain("already exists");
    }

    [Fact]
    public async Task Create_ValidRequest_ReturnsCreatedWithLocationAndEnvelope()
    {
        // Arrange
        var request = new CreateCategoryRequest("Pet Care");

        // Act
        var response = await Client.PostAsJsonAsync(CategoriesUrl, request, ApiJson.Options);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        using var document = await ApiJson.ReadJsonDocumentAsync(response);
        ApiJson.AssertApiResponseEnvelope(document);
        var data = document.RootElement.GetProperty("data");
        data.EnumerateObject().Select(property => property.Name)
            .Should().BeEquivalentTo(["id", "isDefault", "name", "transactionCount"]);
        data.GetProperty("name").GetString().Should().Be("Pet Care");
        data.GetProperty("isDefault").GetBoolean().Should().BeFalse();
        data.GetProperty("transactionCount").GetInt32().Should().Be(0);
        document.RootElement.GetProperty("success").GetBoolean().Should().BeTrue();
        document.RootElement.GetProperty("errors").GetArrayLength().Should().Be(0);
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.OriginalString
            .Should().Be($"{CategoriesUrl}/{data.GetProperty("id").GetGuid()}");
    }

    [Fact]
    public async Task Delete_CategoryUsedByLearnedRule_ReturnsBadRequest()
    {
        // Arrange
        var category = await ApiSeeder.CreateCategoryAsync(Client, "Pet Care");
        await ApiSeeder.CreateLearnedRuleAsync(Client, category.Id, "Pet Store", "PETCO");

        // Act
        var response = await Client.DeleteAsync($"{CategoriesUrl}/{category.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await ApiJson.ReadApiResponseAsync<bool>(response);
        body.Success.Should().BeFalse();
        body.Errors.Should().ContainSingle().Which.Should().Contain("learned rules");
    }

    [Fact]
    public async Task Delete_CategoryWithTransactions_ReturnsBadRequest()
    {
        // Arrange
        var category = await ApiSeeder.CreateCategoryAsync(Client, "Pet Care");
        await SeedTransactionsAsync(category.Id, 1);

        // Act
        var response = await Client.DeleteAsync($"{CategoriesUrl}/{category.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await ApiJson.ReadApiResponseAsync<bool>(response);
        body.Success.Should().BeFalse();
        body.Errors.Should().ContainSingle().Which.Should().Contain("assigned transactions");
    }

    [Fact]
    public async Task Delete_MissingCategory_ReturnsBadRequest()
    {
        // Arrange
        var missingId = Guid.NewGuid();

        // Act
        var response = await Client.DeleteAsync($"{CategoriesUrl}/{missingId}");

        // Assert
        // Pins current behavior (risk R5): a missing category yields 400 rather than 404.
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await ApiJson.ReadApiResponseAsync<bool>(response);
        body.Success.Should().BeFalse();
        body.Errors.Should().ContainSingle().Which.Should().Contain("not found");
    }

    [Fact]
    public async Task Delete_UnusedCustomCategory_ReturnsNoContentWithEmptyBody()
    {
        // Arrange
        var category = await ApiSeeder.CreateCategoryAsync(Client, "Pet Care");

        // Act
        var response = await Client.DeleteAsync($"{CategoriesUrl}/{category.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await response.Content.ReadAsStringAsync()).Should().BeEmpty();
        var followUp = await Client.GetAsync($"{CategoriesUrl}/{category.Id}");
        followUp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Get_ExistingCategory_ReturnsOkWithTransactionCount()
    {
        // Arrange
        var category = await ApiSeeder.CreateCategoryAsync(Client, "Pet Care");
        await SeedTransactionsAsync(category.Id, 2);

        // Act
        var response = await Client.GetAsync($"{CategoriesUrl}/{category.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ApiJson.ReadApiResponseAsync<CategoryResponse>(response);
        body.Success.Should().BeTrue();
        body.Data.Should().Be(new CategoryResponse(category.Id, false, "Pet Care", 2));
    }

    [Fact]
    public async Task Get_MissingCategory_ReturnsNotFound()
    {
        // Arrange
        var missingId = Guid.NewGuid();

        // Act
        var response = await Client.GetAsync($"{CategoriesUrl}/{missingId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var body = await ApiJson.ReadApiResponseAsync<CategoryResponse>(response);
        body.Success.Should().BeFalse();
        body.Errors.Should().ContainSingle().Which.Should().Contain("not found");
    }

    [Fact]
    public async Task List_FreshDatabase_ReturnsSeventeenDefaultsOrderedByName()
    {
        // Arrange
        // (the migrations seed the default categories; custom ones are removed before each test)

        // Act
        var response = await Client.GetAsync(CategoriesUrl);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ApiJson.ReadApiResponseAsync<List<CategoryResponse>>(response);
        body.Success.Should().BeTrue();
        body.Data.Should().HaveCount(17);
        body.Data.Should().OnlyContain(category => category.IsDefault && category.TransactionCount == 0);
        body.Data!.Select(category => category.Name)
            .Should().BeInAscendingOrder(StringComparer.InvariantCultureIgnoreCase);
    }

    [Fact]
    public async Task List_WithAssignedTransactions_ReportsTransactionCounts()
    {
        // Arrange
        var busyCategory = await ApiSeeder.CreateCategoryAsync(Client, "Pet Care");
        var idleCategory = await ApiSeeder.CreateCategoryAsync(Client, "Hobbies");
        await SeedTransactionsAsync(busyCategory.Id, 3);

        // Act
        var response = await Client.GetAsync(CategoriesUrl);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ApiJson.ReadApiResponseAsync<List<CategoryResponse>>(response);
        body.Data.Should().HaveCount(19);
        body.Data!.Single(category => category.Id == busyCategory.Id).TransactionCount.Should().Be(3);
        body.Data!.Single(category => category.Id == idleCategory.Id).TransactionCount.Should().Be(0);
    }

    [Fact]
    public async Task Update_BlankName_ReturnsBadRequest()
    {
        // Arrange
        var category = await ApiSeeder.CreateCategoryAsync(Client, "Pet Care");

        // Act
        var response = await Client.PutAsJsonAsync(
            $"{CategoriesUrl}/{category.Id}",
            new UpdateCategoryRequest(""),
            ApiJson.Options);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await ApiJson.ReadApiResponseAsync<CategoryResponse>(response);
        body.Errors.Should().Contain("Name is required.");
    }

    [Fact]
    public async Task Update_DuplicateName_ReturnsNotFound()
    {
        // Arrange
        await ApiSeeder.CreateCategoryAsync(Client, "Pet Care");
        var hobbies = await ApiSeeder.CreateCategoryAsync(Client, "Hobbies");

        // Act
        var response = await Client.PutAsJsonAsync(
            $"{CategoriesUrl}/{hobbies.Id}",
            new UpdateCategoryRequest("Pet Care"),
            ApiJson.Options);

        // Assert
        // Pins current behavior (risk R5): a duplicate name yields 404 rather than 400/409.
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var body = await ApiJson.ReadApiResponseAsync<CategoryResponse>(response);
        body.Success.Should().BeFalse();
        body.Errors.Should().ContainSingle().Which.Should().Contain("already exists");
    }

    [Fact]
    public async Task Update_MissingCategory_ReturnsNotFound()
    {
        // Arrange
        var missingId = Guid.NewGuid();

        // Act
        var response = await Client.PutAsJsonAsync(
            $"{CategoriesUrl}/{missingId}",
            new UpdateCategoryRequest("Renamed"),
            ApiJson.Options);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_ValidRequest_ReturnsOkWithNewName()
    {
        // Arrange
        var category = await ApiSeeder.CreateCategoryAsync(Client, "Pet Care");
        await SeedTransactionsAsync(category.Id, 1);

        // Act
        var response = await Client.PutAsJsonAsync(
            $"{CategoriesUrl}/{category.Id}",
            new UpdateCategoryRequest("Pets"),
            ApiJson.Options);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ApiJson.ReadApiResponseAsync<CategoryResponse>(response);
        body.Success.Should().BeTrue();
        body.Data.Should().Be(new CategoryResponse(category.Id, false, "Pets", 1));
        var fetched = await Client.GetAsync($"{CategoriesUrl}/{category.Id}");
        (await ApiJson.ReadApiResponseAsync<CategoryResponse>(fetched)).Data!.Name.Should().Be("Pets");
    }

    private async Task<string> GetFirstDefaultCategoryNameAsync()
    {
        var response = await Client.GetAsync(CategoriesUrl);
        var body = await ApiJson.ReadApiResponseAsync<List<CategoryResponse>>(response);
        return body.Data!.First(category => category.IsDefault).Name;
    }

    /// <summary>Seeds committed transactions for the category directly in the database (no endpoint lists them).</summary>
    private async Task SeedTransactionsAsync(Guid categoryId, int count)
    {
        var institutionId = await ApiSeeder.CreateInstitutionAsync(Client);
        var accountId = await ApiSeeder.CreateAccountAsync(Client, institutionId);

        await using var scope = CreateDbContextScope();
        for (var index = 0; index < count; index++)
        {
            scope.DbContext.Transactions.Add(new Transaction
            {
                AccountId = accountId,
                Amount = -10m - index,
                CategoryId = categoryId,
                Date = new DateTime(2026, 1, 10 + index, 0, 0, 0, DateTimeKind.Utc),
                Description = $"SEEDED TRANSACTION {index}",
                Id = Guid.NewGuid(),
                RawCsvRow = $"seeded,{index}",
                Status = TransactionStatus.Committed,
            });
        }

        await scope.DbContext.SaveChangesAsync();
    }
}
