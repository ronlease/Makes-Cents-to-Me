using FluentAssertions;
using MakesCentsToMe.Api.Features.Institutions;
using MakesCentsToMe.Integration.Infrastructure;
using System.Net;
using System.Net.Http.Json;

namespace MakesCentsToMe.Integration.Features.Institutions;

// Feature: Institution endpoint contract
//
// Scenario: Creating an institution returns 201 with a Location header
//   When a valid institution is posted
//   Then the response is 201 Created with an ApiResponse envelope, the institution fields, and a Location header
//
// Scenario: Creating an institution with a blank name is rejected with 400
//
// Scenario: Listing institutions returns them ordered by name with account counts
//
// Scenario: Getting an institution returns 200; a missing institution returns 404
//
// Scenario: Updating an institution returns 200 with the new name
//
// Scenario: Updating with a blank name returns 400; updating a missing institution returns 404
//
// Scenario: Deleting an institution without accounts returns 204 with an empty body
//
// Scenario: Deleting an institution that has accounts is rejected with 400
//
// Scenario: Deleting a missing institution returns 400 (current behavior, risk R5)
[Collection(IntegrationTestCollection.Name)]
public class InstitutionEndpointContractTests(IntegrationTestWebApplicationFactory factory)
    : IntegrationTestBase(factory)
{
    private const string InstitutionsUrl = "/api/v1/institutions";

    [Fact]
    public async Task Create_BlankName_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateInstitutionRequest("  ");

        // Act
        var response = await Client.PostAsJsonAsync(InstitutionsUrl, request, ApiJson.Options);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await ApiJson.ReadApiResponseAsync<InstitutionResponse>(response);
        body.Success.Should().BeFalse();
        body.Errors.Should().Contain("Name is required.");
    }

    [Fact]
    public async Task Create_ValidRequest_ReturnsCreatedWithLocationAndEnvelope()
    {
        // Arrange
        var request = new CreateInstitutionRequest("Evergreen Credit Union");

        // Act
        var response = await Client.PostAsJsonAsync(InstitutionsUrl, request, ApiJson.Options);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        using var document = await ApiJson.ReadJsonDocumentAsync(response);
        ApiJson.AssertApiResponseEnvelope(document);
        var data = document.RootElement.GetProperty("data");
        data.EnumerateObject().Select(property => property.Name)
            .Should().BeEquivalentTo(["id", "name", "accountCount"]);
        data.GetProperty("name").GetString().Should().Be("Evergreen Credit Union");
        data.GetProperty("accountCount").GetInt32().Should().Be(0);
        document.RootElement.GetProperty("success").GetBoolean().Should().BeTrue();
        document.RootElement.GetProperty("errors").GetArrayLength().Should().Be(0);
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.OriginalString
            .Should().Be($"{InstitutionsUrl}/{data.GetProperty("id").GetGuid()}");
    }

    [Fact]
    public async Task Delete_InstitutionWithAccounts_ReturnsBadRequest()
    {
        // Arrange
        var institutionId = await ApiSeeder.CreateInstitutionAsync(Client);
        await ApiSeeder.CreateAccountAsync(Client, institutionId);

        // Act
        var response = await Client.DeleteAsync($"{InstitutionsUrl}/{institutionId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await ApiJson.ReadApiResponseAsync<bool>(response);
        body.Success.Should().BeFalse();
        body.Errors.Should().ContainSingle().Which.Should().Contain("has accounts");
        var followUp = await Client.GetAsync($"{InstitutionsUrl}/{institutionId}");
        followUp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Delete_InstitutionWithoutAccounts_ReturnsNoContentWithEmptyBody()
    {
        // Arrange
        var institutionId = await ApiSeeder.CreateInstitutionAsync(Client);

        // Act
        var response = await Client.DeleteAsync($"{InstitutionsUrl}/{institutionId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await response.Content.ReadAsStringAsync()).Should().BeEmpty();
        var followUp = await Client.GetAsync($"{InstitutionsUrl}/{institutionId}");
        followUp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_MissingInstitution_ReturnsBadRequest()
    {
        // Arrange
        var missingId = Guid.NewGuid();

        // Act
        var response = await Client.DeleteAsync($"{InstitutionsUrl}/{missingId}");

        // Assert
        // Pins current behavior (risk R5): a missing institution yields 400 rather than 404.
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await ApiJson.ReadApiResponseAsync<bool>(response);
        body.Success.Should().BeFalse();
        body.Errors.Should().ContainSingle().Which.Should().Contain("not found");
    }

    [Fact]
    public async Task Get_ExistingInstitution_ReturnsOkWithAccountCount()
    {
        // Arrange
        var institutionId = await ApiSeeder.CreateInstitutionAsync(Client, "Evergreen Credit Union");
        await ApiSeeder.CreateAccountAsync(Client, institutionId, "Checking");
        await ApiSeeder.CreateAccountAsync(Client, institutionId, "Savings");

        // Act
        var response = await Client.GetAsync($"{InstitutionsUrl}/{institutionId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ApiJson.ReadApiResponseAsync<InstitutionResponse>(response);
        body.Success.Should().BeTrue();
        body.Data.Should().Be(new InstitutionResponse(institutionId, "Evergreen Credit Union", 2));
    }

    [Fact]
    public async Task Get_MissingInstitution_ReturnsNotFound()
    {
        // Arrange
        var missingId = Guid.NewGuid();

        // Act
        var response = await Client.GetAsync($"{InstitutionsUrl}/{missingId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var body = await ApiJson.ReadApiResponseAsync<InstitutionResponse>(response);
        body.Success.Should().BeFalse();
        body.Errors.Should().ContainSingle().Which.Should().Contain("not found");
    }

    [Fact]
    public async Task List_MultipleInstitutions_ReturnsOrderedByNameWithAccountCounts()
    {
        // Arrange
        var zetaId = await ApiSeeder.CreateInstitutionAsync(Client, "Zeta Bank");
        var alphaId = await ApiSeeder.CreateInstitutionAsync(Client, "Alpha Bank");
        await ApiSeeder.CreateAccountAsync(Client, zetaId, "Checking");
        await ApiSeeder.CreateAccountAsync(Client, zetaId, "Savings");

        // Act
        var response = await Client.GetAsync(InstitutionsUrl);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ApiJson.ReadApiResponseAsync<List<InstitutionResponse>>(response);
        body.Success.Should().BeTrue();
        body.Data.Should().Equal(
            new InstitutionResponse(alphaId, "Alpha Bank", 0),
            new InstitutionResponse(zetaId, "Zeta Bank", 2));
    }

    [Fact]
    public async Task List_NoInstitutions_ReturnsEmptyList()
    {
        // Arrange
        // (database is reset before each test)

        // Act
        var response = await Client.GetAsync(InstitutionsUrl);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ApiJson.ReadApiResponseAsync<List<InstitutionResponse>>(response);
        body.Success.Should().BeTrue();
        body.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task Update_BlankName_ReturnsBadRequest()
    {
        // Arrange
        var institutionId = await ApiSeeder.CreateInstitutionAsync(Client);

        // Act
        var response = await Client.PutAsJsonAsync(
            $"{InstitutionsUrl}/{institutionId}",
            new UpdateInstitutionRequest(""),
            ApiJson.Options);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await ApiJson.ReadApiResponseAsync<InstitutionResponse>(response);
        body.Errors.Should().Contain("Name is required.");
    }

    [Fact]
    public async Task Update_MissingInstitution_ReturnsNotFound()
    {
        // Arrange
        var missingId = Guid.NewGuid();

        // Act
        var response = await Client.PutAsJsonAsync(
            $"{InstitutionsUrl}/{missingId}",
            new UpdateInstitutionRequest("Renamed"),
            ApiJson.Options);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var body = await ApiJson.ReadApiResponseAsync<InstitutionResponse>(response);
        body.Success.Should().BeFalse();
    }

    [Fact]
    public async Task Update_ValidRequest_ReturnsOkWithNewName()
    {
        // Arrange
        var institutionId = await ApiSeeder.CreateInstitutionAsync(Client, "Old Name");
        await ApiSeeder.CreateAccountAsync(Client, institutionId);

        // Act
        var response = await Client.PutAsJsonAsync(
            $"{InstitutionsUrl}/{institutionId}",
            new UpdateInstitutionRequest("New Name"),
            ApiJson.Options);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ApiJson.ReadApiResponseAsync<InstitutionResponse>(response);
        body.Success.Should().BeTrue();
        body.Data.Should().Be(new InstitutionResponse(institutionId, "New Name", 1));
        var fetched = await Client.GetAsync($"{InstitutionsUrl}/{institutionId}");
        (await ApiJson.ReadApiResponseAsync<InstitutionResponse>(fetched)).Data!.Name.Should().Be("New Name");
    }
}
