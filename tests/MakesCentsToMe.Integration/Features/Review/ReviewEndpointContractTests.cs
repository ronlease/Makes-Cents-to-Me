using FluentAssertions;
using MakesCentsToMe.Api.Features.Import;
using MakesCentsToMe.Api.Features.LearnedRules;
using MakesCentsToMe.Api.Features.Review;
using MakesCentsToMe.Api.Models.Entities;
using MakesCentsToMe.Integration.Infrastructure;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Http.Json;

namespace MakesCentsToMe.Integration.Features.Review;

// Feature: Review endpoint contracts (/api/v1/review)
//
// Scenario: List pending transactions
//   Given imported transactions in several statuses and accounts
//   When the review list is requested (optionally filtered by account)
//   Then only PendingReview and PendingAnalysis rows are returned, ordered by date descending then description
//
// Scenario: Accept a transaction
//   Given a PendingReview transaction
//   When it is accepted
//   Then 200 is returned, the status is Committed and the category equals the suggested category
//   And a committed or missing transaction returns 400
//
// Scenario: Accept all pending transactions
//   Given pending transactions in two accounts
//   When accept-all is called for one account
//   Then the number accepted is returned and the other account is untouched
//
// Scenario: Override a suggestion
//   Given a PendingReview transaction
//   When the user overrides the category and vendor
//   Then 200 is returned with a learned rule suggestion when the correction differs from Claude's suggestion
//   And an unknown category, a non-pending transaction, or a missing transaction returns 400
//
// Scenario: Override feeds the learned rule on the next import
//   Given a transaction overridden with a corrected category
//   When the returned suggestion is posted as a learned rule and a matching row is imported
//   Then the new row is categorized by the rule without calling Claude
[Collection(IntegrationTestCollection.Name)]
public class ReviewEndpointContractTests(IntegrationTestWebApplicationFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task Accept_AlreadyCommitted_ReturnsBadRequest()
    {
        // Arrange
        var accountId = await ApiSeeder.CreateAccountWithProfileAsync(Client);
        await ApiSeeder.ProcessImportAsync(Client, accountId, SingleRowCsv(CsvSamples.ShellLine));
        var item = (await ListAsync(accountId)).Single();
        (await Client.PutAsync($"/api/v1/review/{item.Id}/accept", null)).EnsureSuccessStatusCode();

        // Act
        var response = await Client.PutAsync($"/api/v1/review/{item.Id}/accept", null);
        var body = await ApiJson.ReadApiResponseAsync<ReviewTransactionResponse>(response);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        body.Success.Should().BeFalse();
        body.Errors.Should().ContainSingle().Which.Should().Contain("Committed");
    }

    [Fact]
    public async Task Accept_Missing_ReturnsBadRequest()
    {
        // Arrange
        var unknownId = Guid.NewGuid();

        // Act
        var response = await Client.PutAsync($"/api/v1/review/{unknownId}/accept", null);
        var body = await ApiJson.ReadApiResponseAsync<ReviewTransactionResponse>(response);

        // Assert
        // Current behavior: a missing transaction is reported as 400, not 404 (R5).
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        body.Errors.Should().ContainSingle().Which.Should().Contain("not found");
    }

    [Fact]
    public async Task Accept_PendingReview_ReturnsOkAndCommitsSuggestion()
    {
        // Arrange
        var accountId = await ApiSeeder.CreateAccountWithProfileAsync(Client);
        await ApiSeeder.ProcessImportAsync(Client, accountId, SingleRowCsv(CsvSamples.ShellLine));
        var item = (await ListAsync(accountId)).Single();

        // Act
        var response = await Client.PutAsync($"/api/v1/review/{item.Id}/accept", null);
        var body = await ApiJson.ReadApiResponseAsync<ReviewTransactionResponse>(response);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body.Data!.Status.Should().Be("Committed");
        body.Data.CategoryId.Should().Be(item.SuggestedCategoryId);
        body.Data.NormalizedVendor.Should().Be(item.SuggestedNormalizedVendor);
        (await ListAsync(accountId)).Should().BeEmpty();
    }

    [Fact]
    public async Task AcceptAll_ForOneAccount_ReturnsCountAndLeavesOtherAccountUntouched()
    {
        // Arrange
        var firstAccountId = await ApiSeeder.CreateAccountWithProfileAsync(Client, "First Bank", "Checking");
        var secondAccountId = await ApiSeeder.CreateAccountWithProfileAsync(Client, "Second Bank", "Savings");
        await ApiSeeder.ProcessImportAsync(Client, firstAccountId, CsvSamples.FirstImport);
        await ApiSeeder.ProcessImportAsync(Client, secondAccountId, CsvSamples.OverlapImport);

        // Act
        var response = await Client.PostAsync($"/api/v1/review/accept-all?accountId={firstAccountId}", null);
        using var document = await ApiJson.ReadJsonDocumentAsync(response);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        ApiJson.AssertApiResponseEnvelope(document);
        document.RootElement.GetProperty("data").GetInt32().Should().Be(3);
        (await ListAsync(firstAccountId)).Should().BeEmpty();
        (await ListAsync(secondAccountId)).Should().HaveCount(3);
    }

    [Fact]
    public async Task AcceptAll_NothingPending_ReturnsOkWithZero()
    {
        // Arrange
        // (database was reset before the test)

        // Act
        var response = await Client.PostAsync("/api/v1/review/accept-all", null);
        var body = await ApiJson.ReadApiResponseAsync<int>(response);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body.Data.Should().Be(0);
    }

    [Fact]
    public async Task List_CommittedAndPendingAnalysisRows_ExcludesCommittedAndIncludesPendingAnalysis()
    {
        // Arrange
        var accountId = await ApiSeeder.CreateAccountWithProfileAsync(Client);
        await ApiSeeder.ProcessImportAsync(Client, accountId, CsvSamples.FirstImport);
        var items = await ListAsync(accountId);
        var committed = items.Single(item => item.Description == "SHELL OIL 5551");
        var pendingAnalysis = items.Single(item => item.Description == "PAYROLL DEPOSIT");
        (await Client.PutAsync($"/api/v1/review/{committed.Id}/accept", null)).EnsureSuccessStatusCode();
        await using (var scope = CreateDbContextScope())
        {
            await scope.DbContext.Transactions
                .Where(transaction => transaction.Id == pendingAnalysis.Id)
                .ExecuteUpdateAsync(setters => setters.SetProperty(t => t.Status, TransactionStatus.PendingAnalysis));
        }

        // Act
        var result = await ListAsync(accountId);

        // Assert
        result.Select(item => item.Description).Should().BeEquivalentTo("PAYROLL DEPOSIT", "STARBUCKS #123, SEATTLE WA");
        result.Single(item => item.Description == "PAYROLL DEPOSIT").Status.Should().Be("PendingAnalysis");
    }

    [Fact]
    public async Task List_FilteredByAccount_ReturnsOnlyThatAccountsRows()
    {
        // Arrange
        var firstAccountId = await ApiSeeder.CreateAccountWithProfileAsync(Client, "First Bank", "Checking");
        var secondAccountId = await ApiSeeder.CreateAccountWithProfileAsync(Client, "Second Bank", "Savings");
        await ApiSeeder.ProcessImportAsync(Client, firstAccountId, CsvSamples.FirstImport);
        await ApiSeeder.ProcessImportAsync(Client, secondAccountId, SingleRowCsv(CsvSamples.TraderJoesLine));

        // Act
        var filtered = await ListAsync(secondAccountId);
        var all = await ListAsync(null);

        // Assert
        filtered.Should().ContainSingle();
        filtered[0].AccountName.Should().Be("Savings");
        filtered[0].InstitutionName.Should().Be("Second Bank");
        all.Should().HaveCount(4);
    }

    [Fact]
    public async Task List_PendingReviewRows_ReturnsEnvelopeWithSuggestionFieldsOrderedByDateThenDescription()
    {
        // Arrange
        var accountId = await ApiSeeder.CreateAccountWithProfileAsync(Client);
        var csv = string.Join(
            "\n",
            CsvSamples.Header,
            "01/20/2026,ZEBRA MART,-1.00,1.00",
            "01/20/2026,APPLE MART,-2.00,2.00",
            CsvSamples.ShellLine) + "\n";
        await ApiSeeder.ProcessImportAsync(Client, accountId, csv);

        // Act
        var response = await Client.GetAsync($"/api/v1/review?accountId={accountId}");
        using var document = await ApiJson.ReadJsonDocumentAsync(response);
        var items = (await ListAsync(accountId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        ApiJson.AssertApiResponseEnvelope(document);
        items.Select(item => item.Description).Should().Equal("APPLE MART", "ZEBRA MART", "SHELL OIL 5551");
        items.Should().OnlyContain(item => item.Status == "PendingReview");
        items.Should().OnlyContain(item => item.SuggestedCategory == "Dining");
        items.Should().OnlyContain(item => item.SuggestedCategoryId != null);
        items.Should().OnlyContain(item => item.Confidence == 0.9m);
        items.Should().OnlyContain(item => !item.IsAutoCategorized);
        items[0].SuggestedNormalizedVendor.Should().Be("APPLE MART");
        document.RootElement.GetProperty("data")[0].GetProperty("status").GetString().Should().Be("PendingReview");
    }

    [Fact]
    public async Task Override_DifferentFromSuggestion_ReturnsOkWithLearnedRuleSuggestion()
    {
        // Arrange
        var accountId = await ApiSeeder.CreateAccountWithProfileAsync(Client);
        await ApiSeeder.ProcessImportAsync(Client, accountId, SingleRowCsv(CsvSamples.ShellLine));
        var item = (await ListAsync(accountId)).Single();
        var transportationId = await ApiSeeder.GetCategoryIdAsync(Client, "Transportation");

        // Act
        var response = await OverrideAsync(item.Id, transportationId, "Shell");
        var body = await ApiJson.ReadApiResponseAsync<ReviewTransactionResponse>(response);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body.Data!.Status.Should().Be("Committed");
        body.Data.CategoryId.Should().Be(transportationId);
        body.Data.NormalizedVendor.Should().Be("Shell");
        var suggestion = body.Data.LearnedRuleSuggestion;
        suggestion.Should().NotBeNull();
        suggestion!.CategoryId.Should().Be(transportationId);
        suggestion.CategoryName.Should().Be("Transportation");
        suggestion.ExistingLearnedRuleId.Should().BeNull();
        suggestion.NormalizedVendor.Should().Be("Shell");
        suggestion.Pattern.Should().Be("SHELL OIL");
        suggestion.SourceTransactionId.Should().Be(item.Id);
    }

    [Fact]
    public async Task Override_Missing_ReturnsBadRequest()
    {
        // Arrange
        var groceriesId = await ApiSeeder.GetCategoryIdAsync(Client, "Groceries");

        // Act
        var response = await OverrideAsync(Guid.NewGuid(), groceriesId, "Anything");
        var body = await ApiJson.ReadApiResponseAsync<ReviewTransactionResponse>(response);

        // Assert
        // Current behavior: a missing transaction is reported as 400, not 404 (R5).
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        body.Errors.Should().ContainSingle().Which.Should().Contain("not found");
    }

    [Fact]
    public async Task Override_MatchingSuggestion_ReturnsOkWithoutLearnedRuleSuggestion()
    {
        // Arrange
        var accountId = await ApiSeeder.CreateAccountWithProfileAsync(Client);
        await ApiSeeder.ProcessImportAsync(Client, accountId, SingleRowCsv(CsvSamples.ShellLine));
        var item = (await ListAsync(accountId)).Single();

        // Act
        var response = await OverrideAsync(item.Id, item.SuggestedCategoryId, item.SuggestedNormalizedVendor!);
        var body = await ApiJson.ReadApiResponseAsync<ReviewTransactionResponse>(response);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body.Data!.Status.Should().Be("Committed");
        body.Data.LearnedRuleSuggestion.Should().BeNull();
    }

    [Fact]
    public async Task Override_NotPending_ReturnsBadRequest()
    {
        // Arrange
        var accountId = await ApiSeeder.CreateAccountWithProfileAsync(Client);
        await ApiSeeder.ProcessImportAsync(Client, accountId, SingleRowCsv(CsvSamples.ShellLine));
        var item = (await ListAsync(accountId)).Single();
        (await Client.PutAsync($"/api/v1/review/{item.Id}/accept", null)).EnsureSuccessStatusCode();
        var groceriesId = await ApiSeeder.GetCategoryIdAsync(Client, "Groceries");

        // Act
        var response = await OverrideAsync(item.Id, groceriesId, "Shell");
        var body = await ApiJson.ReadApiResponseAsync<ReviewTransactionResponse>(response);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        body.Errors.Should().ContainSingle().Which.Should().Contain("cannot be overridden");
    }

    [Fact]
    public async Task Override_UnknownCategory_ReturnsBadRequestAndLeavesTransactionPending()
    {
        // Arrange
        var accountId = await ApiSeeder.CreateAccountWithProfileAsync(Client);
        await ApiSeeder.ProcessImportAsync(Client, accountId, SingleRowCsv(CsvSamples.ShellLine));
        var item = (await ListAsync(accountId)).Single();

        // Act
        var response = await OverrideAsync(item.Id, Guid.NewGuid(), "Shell");
        var body = await ApiJson.ReadApiResponseAsync<ReviewTransactionResponse>(response);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        body.Errors.Should().ContainSingle().Which.Should().Contain("not found");
        (await ListAsync(accountId)).Should().ContainSingle();
    }

    [Fact]
    public async Task OverrideFlow_SuggestionPostedAsLearnedRule_AutoCategorizesNextImportWithoutCallingClaude()
    {
        // Arrange
        var accountId = await ApiSeeder.CreateAccountWithProfileAsync(Client);
        await ApiSeeder.ProcessImportAsync(Client, accountId, SingleRowCsv(CsvSamples.ShellLine));
        var item = (await ListAsync(accountId)).Single();
        var transportationId = await ApiSeeder.GetCategoryIdAsync(Client, "Transportation");
        var overrideResponse = await OverrideAsync(item.Id, transportationId, "Shell");
        var suggestion = (await ApiJson.ReadApiResponseAsync<ReviewTransactionResponse>(overrideResponse))
            .Data!.LearnedRuleSuggestion!;
        var claudeRequestsBeforeReimport = ClaudeRequestRecorder.Count;

        // Act
        var ruleResponse = await Client.PostAsJsonAsync(
            "/api/v1/learned-rules",
            new CreateLearnedRuleRequest(
                suggestion.CategoryId,
                suggestion.NormalizedVendor,
                suggestion.Pattern,
                suggestion.SourceTransactionId),
            ApiJson.Options);
        var reimport = await ApiSeeder.ProcessImportAsync(
            Client,
            accountId,
            SingleRowCsv("02/01/2026,SHELL OIL 7788,-31.00,900.00"));

        // Assert
        ruleResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        reimport.AutoCategorizedCount.Should().Be(1);
        reimport.TransactionsCreated.Should().Be(1);
        ClaudeRequestRecorder.Count.Should().Be(claudeRequestsBeforeReimport);
        await using var scope = CreateDbContextScope();
        var transaction = await scope.DbContext.Transactions.AsNoTracking()
            .SingleAsync(t => t.Description == "SHELL OIL 7788");
        transaction.Status.Should().Be(TransactionStatus.Committed);
        transaction.IsAutoCategorized.Should().BeTrue();
        transaction.CategoryId.Should().Be(transportationId);
        transaction.NormalizedVendor.Should().Be("Shell");
        transaction.LearnedRuleId.Should().NotBeNull();
        (await ListAsync(accountId)).Should().BeEmpty();
    }

    private async Task<IReadOnlyList<ReviewTransactionResponse>> ListAsync(Guid? accountId)
    {
        var url = accountId.HasValue ? $"/api/v1/review?accountId={accountId}" : "/api/v1/review";
        var response = await Client.GetAsync(url);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await ApiJson.ReadApiResponseAsync<List<ReviewTransactionResponse>>(response)).Data!;
    }

    private async Task<HttpResponseMessage> OverrideAsync(Guid transactionId, Guid? categoryId, string normalizedVendor) =>
        await Client.PutAsJsonAsync(
            $"/api/v1/review/{transactionId}/override",
            new OverrideTransactionRequest(categoryId, normalizedVendor),
            ApiJson.Options);

    private static string SingleRowCsv(string line) => string.Join("\n", CsvSamples.Header, line) + "\n";
}
