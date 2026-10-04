using FluentAssertions;
using MakesCentsToMe.Api.Models.Entities;
using MakesCentsToMe.Integration.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace MakesCentsToMe.Integration.Features.Import;

// Feature: Integration test coverage
//
// Scenario: Deduplication is verified against a real database context
//   Given transactions already exist for an account in the test database
//   When a CSV containing overlapping rows is imported
//   Then duplicate rows are skipped
//   And the reported new and duplicate counts match the persisted state
//
// The scenario is exercised through the following tests:
//   - overlapping rows are skipped and only the new row is created
//   - the reported counts match the persisted transaction count
//   - re-importing the same file creates nothing and does not call Claude again
//   - identical rows within one file keep their multiplicity
//   - the same key on a different account is not a duplicate
//   - an amount stored with scale four still matches a two-decimal CSV value
[Collection(IntegrationTestCollection.Name)]
public class ImportDeduplicationIntegrationTests(IntegrationTestWebApplicationFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task ProcessImport_AmountStoredWithScaleFour_StillMatchesTwoDecimalCsvValue()
    {
        // Arrange
        var accountId = await ApiSeeder.CreateAccountWithProfileAsync(Client);
        await using (var scope = CreateDbContextScope())
        {
            scope.DbContext.Transactions.Add(new Transaction
            {
                AccountId = accountId,
                Amount = -12.5000m,
                Date = new DateTime(2026, 1, 16, 0, 0, 0, DateTimeKind.Utc),
                Description = "SHELL OIL 5551",
                Id = Guid.NewGuid(),
                Principal = -12.5000m,
                RawCsvRow = "01/16/2026,SHELL OIL 5551,-12.50,100.00",
                Status = TransactionStatus.Committed,
            });
            await scope.DbContext.SaveChangesAsync();
        }

        var csv = string.Join("\n", CsvSamples.Header, "01/16/2026,SHELL OIL 5551,-12.50,100.00") + "\n";

        // Act
        var result = await ApiSeeder.ProcessImportAsync(Client, accountId, csv);

        // Assert
        result.DuplicatesSkipped.Should().Be(1);
        result.TransactionsCreated.Should().Be(0);
        (await CountTransactionsAsync(accountId)).Should().Be(1);
    }

    [Fact]
    public async Task ProcessImport_IdenticalRowsWithinFile_PreservesMultiplicity()
    {
        // Arrange
        var accountId = await ApiSeeder.CreateAccountWithProfileAsync(Client);
        var csv = string.Join("\n", CsvSamples.Header, CsvSamples.ShellLine, CsvSamples.ShellLine) + "\n";

        // Act
        var first = await ApiSeeder.ProcessImportAsync(Client, accountId, csv);
        var second = await ApiSeeder.ProcessImportAsync(Client, accountId, csv);

        // Assert
        first.TransactionsCreated.Should().Be(2);
        first.DuplicatesSkipped.Should().Be(0);
        second.TransactionsCreated.Should().Be(0);
        second.DuplicatesSkipped.Should().Be(2);
        (await CountTransactionsAsync(accountId)).Should().Be(2);
    }

    [Fact]
    public async Task ProcessImport_OverlappingCsv_ReportedCountsMatchPersistedState()
    {
        // Arrange
        var accountId = await ApiSeeder.CreateAccountWithProfileAsync(Client);
        var first = await ApiSeeder.ProcessImportAsync(Client, accountId, CsvSamples.FirstImport);

        // Act
        var second = await ApiSeeder.ProcessImportAsync(Client, accountId, CsvSamples.OverlapImport);

        // Assert
        var persistedCount = await CountTransactionsAsync(accountId);
        persistedCount.Should().Be(first.TransactionsCreated + second.TransactionsCreated);
        persistedCount.Should().Be(4);
        second.DuplicatesSkipped.Should().Be(2);
    }

    [Fact]
    public async Task ProcessImport_OverlappingCsv_SkipsDuplicateRows()
    {
        // Arrange
        var accountId = await ApiSeeder.CreateAccountWithProfileAsync(Client);
        await ApiSeeder.ProcessImportAsync(Client, accountId, CsvSamples.FirstImport);

        // Act
        var result = await ApiSeeder.ProcessImportAsync(Client, accountId, CsvSamples.OverlapImport);

        // Assert
        result.DuplicatesSkipped.Should().Be(2);
        result.TransactionsCreated.Should().Be(1);

        await using var scope = CreateDbContextScope();
        var descriptions = await scope.DbContext.Transactions.AsNoTracking()
            .Where(transaction => transaction.AccountId == accountId)
            .Select(transaction => transaction.Description)
            .ToListAsync();
        descriptions.Should().BeEquivalentTo(
            ["STARBUCKS #123, SEATTLE WA", "SHELL OIL 5551", "PAYROLL DEPOSIT", "TRADER JOES #55"]);
    }

    [Fact]
    public async Task ProcessImport_SameCsvTwice_CreatesNothingOnSecondImport()
    {
        // Arrange
        var accountId = await ApiSeeder.CreateAccountWithProfileAsync(Client);
        await ApiSeeder.ProcessImportAsync(Client, accountId, CsvSamples.FirstImport);
        var claudeRequestCount = ClaudeRequestRecorder.Count;

        // Act
        var result = await ApiSeeder.ProcessImportAsync(Client, accountId, CsvSamples.FirstImport);

        // Assert
        result.TransactionsCreated.Should().Be(0);
        result.DuplicatesSkipped.Should().Be(3);
        (await CountTransactionsAsync(accountId)).Should().Be(3);
        ClaudeRequestRecorder.Count.Should().Be(claudeRequestCount);
    }

    [Fact]
    public async Task ProcessImport_SameKeyDifferentAccount_IsNotTreatedAsDuplicate()
    {
        // Arrange
        var firstAccountId = await ApiSeeder.CreateAccountWithProfileAsync(Client, accountName: "Checking");
        var institutionId = await ApiSeeder.CreateInstitutionAsync(Client, "Second Bank");
        var secondAccountId = await ApiSeeder.CreateAccountAsync(Client, institutionId, "Savings");
        await ApiSeeder.SaveImportProfileAsync(Client, secondAccountId);
        await ApiSeeder.ProcessImportAsync(Client, firstAccountId, CsvSamples.FirstImport);

        // Act
        var result = await ApiSeeder.ProcessImportAsync(Client, secondAccountId, CsvSamples.FirstImport);

        // Assert
        result.DuplicatesSkipped.Should().Be(0);
        result.TransactionsCreated.Should().Be(3);
        (await CountTransactionsAsync(firstAccountId)).Should().Be(3);
        (await CountTransactionsAsync(secondAccountId)).Should().Be(3);
    }

    private async Task<int> CountTransactionsAsync(Guid accountId)
    {
        await using var scope = CreateDbContextScope();
        return await scope.DbContext.Transactions.AsNoTracking()
            .CountAsync(transaction => transaction.AccountId == accountId);
    }
}
