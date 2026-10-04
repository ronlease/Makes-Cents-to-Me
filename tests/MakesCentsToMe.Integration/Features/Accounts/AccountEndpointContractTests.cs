using FluentAssertions;
using MakesCentsToMe.Api.Features.Accounts;
using MakesCentsToMe.Api.Models.Entities;
using MakesCentsToMe.Integration.Infrastructure;
using System.Net;
using System.Net.Http.Json;

namespace MakesCentsToMe.Integration.Features.Accounts;

// Feature: Account endpoint contract
//
// Scenario: Creating an account returns 201 with a Location header
//   Given an institution exists
//   When a valid account is posted
//   Then the response is 201 Created with an ApiResponse envelope, the account fields, and a Location header
//
// Scenario: Creating an account with a blank name is rejected
//   Given an institution exists
//   When an account with a blank name is posted
//   Then the response is 400 Bad Request
//
// Scenario: Creating an account for an unknown institution is rejected
//   When an account is posted for an institution that does not exist
//   Then the response is 400 Bad Request
//
// Scenario: Creating a duplicate account name within an institution is rejected
//   Given an institution has an account named Checking
//   When another account named Checking is posted to the same institution
//   Then the response is 400 Bad Request
//
// Scenario: Listing accounts returns them ordered by name
//   Given an institution has several accounts
//   When the accounts are listed
//   Then the response is 200 OK with the accounts ordered by name
//
// Scenario: Listing accounts for an unknown institution returns 404
//
// Scenario: Getting an account reflects whether it has an import profile
//   Given one account with an import profile and one without
//   When each account is fetched
//   Then hasImportProfile is true and false respectively
//
// Scenario: Getting a missing account, or one under another institution, returns 404
//
// Scenario: Updating an account returns 200 with the new values
//   Given an existing account
//   When it is updated with a new name and type
//   Then the response is 200 OK with the new values
//
// Scenario: Updating with a blank name returns 400; updating a missing account returns 404
//
// Scenario: Updating to a duplicate name returns 404 (current behavior, risk R5)
//
// Scenario: Deleting an account without transactions returns 204 with an empty body
//
// Scenario: Deleting an account that has transactions is rejected with 400
//
// Scenario: Deleting a missing account returns 400 (current behavior, risk R5)
[Collection(IntegrationTestCollection.Name)]
public class AccountEndpointContractTests(IntegrationTestWebApplicationFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task Create_BlankName_ReturnsBadRequest()
    {
        // Arrange
        var institutionId = await ApiSeeder.CreateInstitutionAsync(Client);

        // Act
        var response = await Client.PostAsJsonAsync(
            AccountsUrl(institutionId),
            new CreateAccountRequest("   ", AccountType.Checking),
            ApiJson.Options);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await ApiJson.ReadApiResponseAsync<AccountResponse>(response);
        body.Success.Should().BeFalse();
        body.Errors.Should().Contain("Name is required.");
    }

    [Fact]
    public async Task Create_DuplicateNameInDifferentInstitution_ReturnsCreated()
    {
        // Arrange
        var firstInstitutionId = await ApiSeeder.CreateInstitutionAsync(Client, "First Bank");
        var secondInstitutionId = await ApiSeeder.CreateInstitutionAsync(Client, "Second Bank");
        await ApiSeeder.CreateAccountAsync(Client, firstInstitutionId, "Checking");

        // Act
        var response = await Client.PostAsJsonAsync(
            AccountsUrl(secondInstitutionId),
            new CreateAccountRequest("Checking", AccountType.Checking),
            ApiJson.Options);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Create_DuplicateNameInSameInstitution_ReturnsBadRequest()
    {
        // Arrange
        var institutionId = await ApiSeeder.CreateInstitutionAsync(Client);
        await ApiSeeder.CreateAccountAsync(Client, institutionId, "Checking");

        // Act
        var response = await Client.PostAsJsonAsync(
            AccountsUrl(institutionId),
            new CreateAccountRequest("Checking", AccountType.Savings),
            ApiJson.Options);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await ApiJson.ReadApiResponseAsync<AccountResponse>(response);
        body.Success.Should().BeFalse();
        body.Errors.Should().ContainSingle().Which.Should().Contain("already exists");
    }

    [Fact]
    public async Task Create_UnknownInstitution_ReturnsBadRequest()
    {
        // Arrange
        var unknownInstitutionId = Guid.NewGuid();

        // Act
        var response = await Client.PostAsJsonAsync(
            AccountsUrl(unknownInstitutionId),
            new CreateAccountRequest("Checking", AccountType.Checking),
            ApiJson.Options);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await ApiJson.ReadApiResponseAsync<AccountResponse>(response);
        body.Success.Should().BeFalse();
        body.Errors.Should().ContainSingle().Which.Should().Contain("not found");
    }

    [Fact]
    public async Task Create_ValidRequest_ReturnsCreatedWithLocationAndEnvelope()
    {
        // Arrange
        var institutionId = await ApiSeeder.CreateInstitutionAsync(Client);

        // Act
        var response = await Client.PostAsJsonAsync(
            AccountsUrl(institutionId),
            new CreateAccountRequest("Everyday Checking", AccountType.CreditCard),
            ApiJson.Options);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        using var document = await ApiJson.ReadJsonDocumentAsync(response);
        ApiJson.AssertApiResponseEnvelope(document);
        var data = document.RootElement.GetProperty("data");
        data.EnumerateObject().Select(property => property.Name)
            .Should().BeEquivalentTo(["id", "name", "accountType", "hasImportProfile"]);
        data.GetProperty("name").GetString().Should().Be("Everyday Checking");
        data.GetProperty("accountType").GetString().Should().Be("CreditCard");
        data.GetProperty("hasImportProfile").GetBoolean().Should().BeFalse();
        document.RootElement.GetProperty("success").GetBoolean().Should().BeTrue();
        document.RootElement.GetProperty("errors").GetArrayLength().Should().Be(0);
        var accountId = data.GetProperty("id").GetGuid();
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.OriginalString.Should().Be($"{AccountsUrl(institutionId)}/{accountId}");
    }

    [Fact]
    public async Task Delete_AccountWithoutTransactions_ReturnsNoContentWithEmptyBody()
    {
        // Arrange
        var institutionId = await ApiSeeder.CreateInstitutionAsync(Client);
        var accountId = await ApiSeeder.CreateAccountAsync(Client, institutionId);

        // Act
        var response = await Client.DeleteAsync($"{AccountsUrl(institutionId)}/{accountId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await response.Content.ReadAsStringAsync()).Should().BeEmpty();
        var followUp = await Client.GetAsync($"{AccountsUrl(institutionId)}/{accountId}");
        followUp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_AccountWithTransactions_ReturnsBadRequest()
    {
        // Arrange
        var institutionId = await ApiSeeder.CreateInstitutionAsync(Client);
        var accountId = await ApiSeeder.CreateAccountAsync(Client, institutionId);
        await ApiSeeder.SaveImportProfileAsync(Client, accountId);
        await ApiSeeder.ProcessImportAsync(Client, accountId, CsvSamples.FirstImport);

        // Act
        var response = await Client.DeleteAsync($"{AccountsUrl(institutionId)}/{accountId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await ApiJson.ReadApiResponseAsync<bool>(response);
        body.Success.Should().BeFalse();
        body.Errors.Should().ContainSingle().Which.Should().Contain("has transactions");
        var followUp = await Client.GetAsync($"{AccountsUrl(institutionId)}/{accountId}");
        followUp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Delete_MissingAccount_ReturnsBadRequest()
    {
        // Arrange
        var institutionId = await ApiSeeder.CreateInstitutionAsync(Client);

        // Act
        var response = await Client.DeleteAsync($"{AccountsUrl(institutionId)}/{Guid.NewGuid()}");

        // Assert
        // Pins current behavior (risk R5): a missing account yields 400 rather than 404.
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await ApiJson.ReadApiResponseAsync<bool>(response);
        body.Success.Should().BeFalse();
        body.Errors.Should().ContainSingle().Which.Should().Contain("not found");
    }

    [Fact]
    public async Task Get_AccountUnderDifferentInstitution_ReturnsNotFound()
    {
        // Arrange
        var ownerInstitutionId = await ApiSeeder.CreateInstitutionAsync(Client, "Owner Bank");
        var otherInstitutionId = await ApiSeeder.CreateInstitutionAsync(Client, "Other Bank");
        var accountId = await ApiSeeder.CreateAccountAsync(Client, ownerInstitutionId);

        // Act
        var response = await Client.GetAsync($"{AccountsUrl(otherInstitutionId)}/{accountId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Get_AccountWithImportProfile_ReturnsHasImportProfileTrue()
    {
        // Arrange
        var institutionId = await ApiSeeder.CreateInstitutionAsync(Client);
        var accountId = await ApiSeeder.CreateAccountAsync(Client, institutionId, "Checking", AccountType.Savings);
        await ApiSeeder.SaveImportProfileAsync(Client, accountId);

        // Act
        var response = await Client.GetAsync($"{AccountsUrl(institutionId)}/{accountId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ApiJson.ReadApiResponseAsync<AccountResponse>(response);
        body.Success.Should().BeTrue();
        body.Data.Should().Be(new AccountResponse(accountId, "Checking", AccountType.Savings, true));
    }

    [Fact]
    public async Task Get_AccountWithoutImportProfile_ReturnsHasImportProfileFalse()
    {
        // Arrange
        var institutionId = await ApiSeeder.CreateInstitutionAsync(Client);
        var accountId = await ApiSeeder.CreateAccountAsync(Client, institutionId, "Checking");

        // Act
        var response = await Client.GetAsync($"{AccountsUrl(institutionId)}/{accountId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ApiJson.ReadApiResponseAsync<AccountResponse>(response);
        body.Data.Should().Be(new AccountResponse(accountId, "Checking", AccountType.Checking, false));
    }

    [Fact]
    public async Task Get_MissingAccount_ReturnsNotFound()
    {
        // Arrange
        var institutionId = await ApiSeeder.CreateInstitutionAsync(Client);

        // Act
        var response = await Client.GetAsync($"{AccountsUrl(institutionId)}/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var body = await ApiJson.ReadApiResponseAsync<AccountResponse>(response);
        body.Success.Should().BeFalse();
        body.Errors.Should().ContainSingle().Which.Should().Contain("not found");
    }

    [Fact]
    public async Task List_InstitutionWithAccounts_ReturnsAccountsOrderedByName()
    {
        // Arrange
        var institutionId = await ApiSeeder.CreateInstitutionAsync(Client);
        await ApiSeeder.CreateAccountAsync(Client, institutionId, "Savings", AccountType.Savings);
        await ApiSeeder.CreateAccountAsync(Client, institutionId, "Checking");
        await ApiSeeder.CreateAccountAsync(Client, institutionId, "Visa", AccountType.CreditCard);
        var otherInstitutionId = await ApiSeeder.CreateInstitutionAsync(Client, "Other Bank");
        await ApiSeeder.CreateAccountAsync(Client, otherInstitutionId, "Aardvark");

        // Act
        var response = await Client.GetAsync(AccountsUrl(institutionId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ApiJson.ReadApiResponseAsync<List<AccountResponse>>(response);
        body.Success.Should().BeTrue();
        body.Data!.Select(account => account.Name).Should().Equal("Checking", "Savings", "Visa");
        body.Data!.Select(account => account.AccountType)
            .Should().Equal(AccountType.Checking, AccountType.Savings, AccountType.CreditCard);
    }

    [Fact]
    public async Task List_UnknownInstitution_ReturnsNotFound()
    {
        // Arrange
        var unknownInstitutionId = Guid.NewGuid();

        // Act
        var response = await Client.GetAsync(AccountsUrl(unknownInstitutionId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var body = await ApiJson.ReadApiResponseAsync<List<AccountResponse>>(response);
        body.Success.Should().BeFalse();
        body.Errors.Should().ContainSingle().Which.Should().Contain("not found");
    }

    [Fact]
    public async Task Update_BlankName_ReturnsBadRequest()
    {
        // Arrange
        var institutionId = await ApiSeeder.CreateInstitutionAsync(Client);
        var accountId = await ApiSeeder.CreateAccountAsync(Client, institutionId);

        // Act
        var response = await Client.PutAsJsonAsync(
            $"{AccountsUrl(institutionId)}/{accountId}",
            new UpdateAccountRequest("", AccountType.Checking),
            ApiJson.Options);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await ApiJson.ReadApiResponseAsync<AccountResponse>(response);
        body.Errors.Should().Contain("Name is required.");
    }

    [Fact]
    public async Task Update_DuplicateName_ReturnsNotFound()
    {
        // Arrange
        var institutionId = await ApiSeeder.CreateInstitutionAsync(Client);
        await ApiSeeder.CreateAccountAsync(Client, institutionId, "Checking");
        var savingsId = await ApiSeeder.CreateAccountAsync(Client, institutionId, "Savings");

        // Act
        var response = await Client.PutAsJsonAsync(
            $"{AccountsUrl(institutionId)}/{savingsId}",
            new UpdateAccountRequest("Checking", AccountType.Savings),
            ApiJson.Options);

        // Assert
        // Pins current behavior (risk R5): a duplicate name yields 404 rather than 400/409.
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var body = await ApiJson.ReadApiResponseAsync<AccountResponse>(response);
        body.Success.Should().BeFalse();
        body.Errors.Should().ContainSingle().Which.Should().Contain("already exists");
    }

    [Fact]
    public async Task Update_MissingAccount_ReturnsNotFound()
    {
        // Arrange
        var institutionId = await ApiSeeder.CreateInstitutionAsync(Client);

        // Act
        var response = await Client.PutAsJsonAsync(
            $"{AccountsUrl(institutionId)}/{Guid.NewGuid()}",
            new UpdateAccountRequest("Checking", AccountType.Checking),
            ApiJson.Options);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_ValidRequest_ReturnsOkWithNewValues()
    {
        // Arrange
        var institutionId = await ApiSeeder.CreateInstitutionAsync(Client);
        var accountId = await ApiSeeder.CreateAccountAsync(Client, institutionId, "Checking");
        await ApiSeeder.SaveImportProfileAsync(Client, accountId);

        // Act
        var response = await Client.PutAsJsonAsync(
            $"{AccountsUrl(institutionId)}/{accountId}",
            new UpdateAccountRequest("Rewards Visa", AccountType.CreditCard),
            ApiJson.Options);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ApiJson.ReadApiResponseAsync<AccountResponse>(response);
        body.Success.Should().BeTrue();
        body.Data.Should().Be(new AccountResponse(accountId, "Rewards Visa", AccountType.CreditCard, true));
        var fetched = await Client.GetAsync($"{AccountsUrl(institutionId)}/{accountId}");
        (await ApiJson.ReadApiResponseAsync<AccountResponse>(fetched)).Data!.Name.Should().Be("Rewards Visa");
    }

    private static string AccountsUrl(Guid institutionId) => $"/api/v1/institutions/{institutionId}/accounts";
}
