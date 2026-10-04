using FluentAssertions;
using MakesCentsToMe.Api.Features.Import;
using MakesCentsToMe.Api.Features.Review;
using MakesCentsToMe.Api.Models.Entities;
using MakesCentsToMe.Integration.Infrastructure;
using Microsoft.EntityFrameworkCore;
using System.Net;

namespace MakesCentsToMe.Integration.Features.Import;

// Feature: Integration test coverage
//
// Scenario: Import pipeline is covered end to end
//   Given an institution, an account, and a saved import profile exist in the test database
//   When a CSV file is imported through the pipeline
//   Then the parsed transactions are persisted with their raw data preserved verbatim
//   And the resulting transactions are retrievable through the API layer
//
// The scenario is exercised through the following tests:
//   - the response reports created and skipped row counts
//   - the raw CSV line is stored verbatim (quotes and embedded commas intact)
//   - the raw column data round-trips through the jsonb column
//   - decimals and UTC dates are stored faithfully
//   - the transactions are retrievable through GET /api/v1/review
//   - a description matching a learned rule is auto-categorized without calling Claude
//   - a profile without balance data computes running balances from the opening balance
[Collection(IntegrationTestCollection.Name)]
public class ImportPipelineIntegrationTests(IntegrationTestWebApplicationFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task ProcessImport_DescriptionMatchesLearnedRule_AutoCategorizesWithoutCallingClaude()
    {
        // Arrange
        var accountId = await ApiSeeder.CreateAccountWithProfileAsync(Client);
        var categoryId = await ApiSeeder.GetCategoryIdAsync(Client, "Transportation");
        var rule = await ApiSeeder.CreateLearnedRuleAsync(Client, categoryId, "Shell", "SHELL OIL");

        // Act
        var result = await ApiSeeder.ProcessImportAsync(Client, accountId, CsvSamples.FirstImport);

        // Assert
        result.AutoCategorizedCount.Should().Be(1);
        result.TransactionsCreated.Should().Be(3);

        await using var scope = CreateDbContextScope();
        var shell = await scope.DbContext.Transactions.AsNoTracking()
            .SingleAsync(transaction => transaction.AccountId == accountId && transaction.Description == "SHELL OIL 5551");
        shell.Status.Should().Be(TransactionStatus.Committed);
        shell.IsAutoCategorized.Should().BeTrue();
        shell.LearnedRuleId.Should().Be(rule.Id);
        shell.CategoryId.Should().Be(categoryId);
        shell.NormalizedVendor.Should().Be("Shell");

        ClaudeRequestRecorder.Descriptions.Should().NotContain("SHELL OIL 5551");
        ClaudeRequestRecorder.Descriptions.Should().HaveCount(2);
    }

    [Fact]
    public async Task ProcessImport_NoBalanceProfileWithOpeningBalance_ComputesRunningBalances()
    {
        // Arrange
        var accountId = await ApiSeeder.CreateAccountWithProfileAsync(Client, profile: CsvSamples.NoBalanceProfile);

        // Act
        var result = await ApiSeeder.ProcessImportAsync(Client, accountId, CsvSamples.NoBalanceImport, 100.00m);

        // Assert
        result.TransactionsCreated.Should().Be(2);

        var transactions = await LoadTransactionsAsync(accountId);
        transactions.Select(transaction => (transaction.Description, transaction.Balance)).Should().Equal(
            ("COFFEE SHOP", (decimal?)95.00m),
            ("BOOKSTORE", (decimal?)75.00m));
    }

    [Fact]
    public async Task ProcessImport_ValidCsv_PersistsDecimalAndUtcDateFaithfully()
    {
        // Arrange
        var accountId = await ApiSeeder.CreateAccountWithProfileAsync(Client);

        // Act
        await ApiSeeder.ProcessImportAsync(Client, accountId, CsvSamples.FirstImport);

        // Assert
        var transactions = await LoadTransactionsAsync(accountId);
        var starbucks = transactions.Single(transaction => transaction.Description == "STARBUCKS #123, SEATTLE WA");
        starbucks.Amount.Should().Be(-4.75m);
        starbucks.Balance.Should().Be(995.25m);
        starbucks.Date.Should().Be(new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc));
        transactions.Should().OnlyContain(transaction => transaction.Date.Kind == DateTimeKind.Utc);
        transactions.Single(transaction => transaction.Description == "PAYROLL DEPOSIT").Amount.Should().Be(2000.00m);
    }

    [Fact]
    public async Task ProcessImport_ValidCsv_PersistsRawCsvRowVerbatim()
    {
        // Arrange
        var accountId = await ApiSeeder.CreateAccountWithProfileAsync(Client);

        // Act
        await ApiSeeder.ProcessImportAsync(Client, accountId, CsvSamples.FirstImport);

        // Assert
        var transactions = await LoadTransactionsAsync(accountId);
        transactions.Select(transaction => transaction.RawCsvRow).Should().BeEquivalentTo(
            [CsvSamples.QuotedCommaLine, CsvSamples.ShellLine, CsvSamples.PayrollLine]);
    }

    [Fact]
    public async Task ProcessImport_ValidCsv_PersistsRawDataRoundTripThroughJsonb()
    {
        // Arrange
        var accountId = await ApiSeeder.CreateAccountWithProfileAsync(Client);

        // Act
        await ApiSeeder.ProcessImportAsync(Client, accountId, CsvSamples.FirstImport);

        // Assert
        var transactions = await LoadTransactionsAsync(accountId);
        var starbucks = transactions.Single(transaction => transaction.RawCsvRow == CsvSamples.QuotedCommaLine);
        starbucks.RawData.Should().BeEquivalentTo(new Dictionary<string, string>
        {
            ["Amount"] = "-4.75",
            ["Balance"] = "995.25",
            ["Date"] = "01/15/2026",
            ["Description"] = "STARBUCKS #123, SEATTLE WA",
        });
        transactions.Should().OnlyContain(transaction => transaction.RawData.Count == 4);
    }

    [Fact]
    public async Task ProcessImport_ValidCsv_ReturnsCreatedAndSkippedCounts()
    {
        // Arrange
        var accountId = await ApiSeeder.CreateAccountWithProfileAsync(Client);

        // Act
        var response = await ApiSeeder.PostProcessImportAsync(Client, accountId, CsvSamples.FirstImport);
        var body = await ApiJson.ReadApiResponseAsync<ProcessImportResponse>(response);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body.Success.Should().BeTrue();
        body.Data!.TransactionsCreated.Should().Be(3);
        body.Data.RowsSkipped.Should().Be(1);
        body.Data.DuplicatesSkipped.Should().Be(0);
        body.Data.AutoCategorizedCount.Should().Be(0);
    }

    [Fact]
    public async Task ProcessImport_ValidCsv_TransactionsRetrievableThroughReviewEndpoint()
    {
        // Arrange
        var accountId = await ApiSeeder.CreateAccountWithProfileAsync(Client, "Pioneer Credit Union", "Everyday Checking");

        // Act
        await ApiSeeder.ProcessImportAsync(Client, accountId, CsvSamples.FirstImport);
        var response = await Client.GetAsync($"/api/v1/review?accountId={accountId}");
        var items = (await ApiJson.ReadApiResponseAsync<List<ReviewTransactionResponse>>(response)).Data!;

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        items.Should().HaveCount(3);
        items.Should().OnlyContain(item => item.Status == "PendingReview");
        items.Should().OnlyContain(item => item.SuggestedCategory == "Dining");
        items.Should().OnlyContain(item => item.SuggestedCategoryId != null);
        items.Should().OnlyContain(item => item.AccountName == "Everyday Checking");
        items.Should().OnlyContain(item => item.InstitutionName == "Pioneer Credit Union");
        items.Select(item => item.Description).Should().BeEquivalentTo(
            ["STARBUCKS #123, SEATTLE WA", "SHELL OIL 5551", "PAYROLL DEPOSIT"]);
    }

    /// <summary>Loads the account's transactions ordered by date then description for stable assertions.</summary>
    private async Task<List<Transaction>> LoadTransactionsAsync(Guid accountId)
    {
        await using var scope = CreateDbContextScope();
        var transactions = await scope.DbContext.Transactions.AsNoTracking()
            .Where(transaction => transaction.AccountId == accountId)
            .ToListAsync();

        return transactions
            .OrderBy(transaction => transaction.Date)
            .ThenBy(transaction => transaction.Description, StringComparer.Ordinal)
            .ToList();
    }
}
