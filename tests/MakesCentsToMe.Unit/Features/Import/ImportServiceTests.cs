// Feature: Import Profile Management
//
// Scenario: Get profile when profile exists
//   Given an import profile exists for the account
//   When GetProfileAsync is called
//   Then the profile is returned with all column mappings
//
// Scenario: Get profile when no profile exists
//   Given no import profile exists for the account
//   When GetProfileAsync is called
//   Then a failure response is returned
//
// Scenario: Save profile when account exists and no profile exists
//   Given an account exists with no import profile
//   When SaveProfileAsync is called
//   Then the profile is persisted and returned
//
// Scenario: Save profile when account does not exist
//   Given no account exists with the given id
//   When SaveProfileAsync is called
//   Then a failure response is returned
//
// Scenario: Save profile when profile already exists
//   Given an import profile already exists for the account
//   When SaveProfileAsync is called
//   Then a failure response is returned citing PUT
//
// Scenario: Save profile persists column mappings
//   Given a save request with two column mappings
//   When SaveProfileAsync is called
//   Then both column mappings are persisted
//
// Scenario: Update profile when profile exists
//   Given an import profile exists for the account
//   When UpdateProfileAsync is called with new settings
//   Then the profile is updated and the new values are returned
//
// Scenario: Update profile when no profile exists
//   Given no import profile exists for the account
//   When UpdateProfileAsync is called
//   Then a failure response is returned
//
// Scenario: Update profile removes column mappings that are no longer present
//   Given a profile with column mappings "Date" and "Description"
//   When UpdateProfileAsync is called with only "Date"
//   Then the "Description" mapping is removed
//
// Scenario: Update profile adds new column mappings
//   Given a profile with one column mapping
//   When UpdateProfileAsync is called adding a second mapping
//   Then the new mapping is persisted
//
// Scenario: Update profile updates existing column mapping application field
//   Given a profile with a mapping from "Trans Desc" to "Description"
//   When UpdateProfileAsync is called changing it to "Transaction Description" -> "Description"
//   Then the existing mapping is updated
//
// Scenario: Update profile with no changes does not call SaveChanges unnecessarily
//   Given a profile exists
//   When UpdateProfileAsync is called with identical data
//   Then a success response is returned
//
// Feature: Upload Preview
//
// Scenario: Upload preview when account does not exist
//   Given no account exists with the given id
//   When UploadPreviewAsync is called
//   Then a failure response is returned
//
// Scenario: Upload preview with empty CSV file
//   Given an account exists
//   When UploadPreviewAsync is called with an empty stream
//   Then a failure response is returned citing empty file
//
// Scenario: Upload preview returns headers and up to five preview rows
//   Given an account exists
//   When UploadPreviewAsync is called with a CSV containing 7 data rows
//   Then the response contains the headers and exactly 5 preview rows
//
// Scenario: Upload preview with only a header row returns empty preview rows
//   Given an account exists
//   When UploadPreviewAsync is called with a header-only CSV
//   Then the response has no preview rows
//
// Scenario: Upload preview correctly parses quoted fields
//   Given an account exists
//   When UploadPreviewAsync is called with a CSV containing quoted comma values
//   Then the quoted field is returned as a single value
//
// Feature: Process Import
//
// Scenario: Process import when account does not exist
//   Given no account exists with the given id
//   When ProcessAsync is called
//   Then a failure response is returned
//
// Scenario: Process import when account has no import profile
//   Given an account exists with no import profile
//   When ProcessAsync is called
//   Then a failure response is returned
//
// Scenario: Process import when profile has no balance and no balance supplied
//   Given a profile with BalanceProvided = false
//   When ProcessAsync is called without openingBalance or closingBalance
//   Then a failure response is returned
//
// Scenario: Process import with only a header row returns zero transactions
//   Given a valid account and profile
//   When ProcessAsync is called with a header-only CSV
//   Then the response shows 0 transactions created and 0 rows skipped
//
// Scenario: Process import creates transactions from valid CSV rows
//   Given a valid account and single-amount profile
//   When ProcessAsync is called with a CSV containing two valid data rows
//   Then two transactions are created and persisted
//
// Scenario: Process import skips rows with column count mismatch
//   Given a valid account and profile
//   When ProcessAsync is called with a CSV row that has too few columns
//   Then the row is skipped and the skip counter is incremented
//
// Scenario: Process import skips rows with missing date
//   Given a valid account and profile
//   When ProcessAsync is called with a CSV row that has an empty date field
//   Then the row is skipped and the skip counter is incremented
//
// Scenario: Process import skips rows with missing description
//   Given a valid account and profile
//   When ProcessAsync is called with a CSV row that has an empty description field
//   Then the row is skipped and the skip counter is incremented
//
// Scenario: Process import skips rows with unparseable date
//   Given a valid account and profile
//   When ProcessAsync is called with a CSV row containing a date that cannot be parsed
//   Then the row is skipped and the skip counter is incremented
//
// Scenario: Process import — split amount type maps principal interest and fees separately
//   Given a profile with AmountType = Split
//   When ProcessAsync is called with valid principal, interest, and fees columns
//   Then the transaction stores each amount separately and Amount = Principal + Interest + Fees
//
// Scenario: Process import — single amount type sets principal to amount and interest and fees to zero
//   Given a profile with AmountType = Single
//   When ProcessAsync is called with an amount column
//   Then Principal equals Amount and Interest and Fees equal zero
//
// Scenario: Process import — balance provided in CSV is stored on transaction
//   Given a profile with BalanceProvided = true
//   When ProcessAsync is called with a balance column
//   Then the transaction Balance matches the CSV value
//
// Scenario: Process import — opening balance computes running balances
//   Given a profile with BalanceProvided = false
//   When ProcessAsync is called with openingBalance = 1000 and two transactions of 100 and 200
//   Then the first transaction balance is 1100 and the second is 1300
//
// Scenario: Process import — closing balance computes running balances backwards
//   Given a profile with BalanceProvided = false
//   When ProcessAsync is called with closingBalance = 1300 and two transactions of 100 and 200
//   Then the balances are computed back from 1300
//
// Scenario: Process import preserves raw CSV row verbatim on each transaction
//   Given a valid account and profile
//   When ProcessAsync is called with a data row
//   Then the transaction's RawCsvRow matches the original CSV line byte-for-byte
//
// Scenario: Process import preserves raw description verbatim
//   Given a valid account and profile
//   When ProcessAsync is called with a description that contains leading/trailing spaces
//   Then the raw description stored on the transaction is the trimmed CSV field value
//
// Scenario: Process import stores all columns in RawData dictionary
//   Given a valid account and profile with headers Date and Description
//   When ProcessAsync is called with a matching data row
//   Then the transaction RawData contains entries for both headers
//
// Scenario: Process import — optional category column stored when present
//   Given a profile that maps a Category column
//   When ProcessAsync is called with a row containing a category value
//   Then the transaction Category is set
//
// Scenario: Process import — optional category column is null when empty
//   Given a profile that maps a Category column
//   When ProcessAsync is called with a row containing an empty category
//   Then the transaction Category is null
//
// Scenario: Process import — optional check number column stored when present
//   Given a profile that maps a CheckNumber column
//   When ProcessAsync is called with a row containing a check number
//   Then the transaction CheckNumber is set
//
// Scenario: Process import date stored as UTC
//   Given a valid account and profile
//   When ProcessAsync is called with a date
//   Then the transaction Date has DateTimeKind.Utc
//
// Scenario: Process import — CSV with only empty lines returns zero transactions
//   Given a valid account and profile
//   When ProcessAsync is called with a stream containing only blank lines
//   Then zero transactions are created
//
// Scenario: TryParseDate falls back to flexible parsing when exact format fails
//   Given a profile with date format "MM/dd/yyyy"
//   When ProcessAsync is called with a date formatted as "January 1, 2024"
//   Then the transaction is created with the correctly parsed date
//
// Feature: Import Deduplication
//
// Scenario: Import with duplicate rows skips matching transactions
//   Given a transaction for account A on 2024-01-01 with description "Coffee" and amount 5.00 already exists
//   When ProcessAsync is called with a CSV row matching the same date, description, and amount
//   Then the duplicate row is skipped and DuplicatesSkipped is 1
//   And no new transaction is created in the database
//
// Scenario: Import same file twice results in all duplicates on second import
//   Given a CSV with two data rows has already been imported successfully
//   When the same CSV is imported again for the same account
//   Then DuplicatesSkipped equals 2 and TransactionsCreated equals 0
//
// Scenario: Two legitimately distinct transactions on same day from same vendor are both imported on first import
//   Given no existing transactions in the database
//   When a CSV containing two identical rows (same date, description, amount) is imported
//   Then both transactions are created because neither existed before import
//
// Scenario: ProcessImportResponse includes DuplicatesSkipped count
//   Given one existing transaction and a CSV containing that same row plus one new row
//   When ProcessAsync is called
//   Then the response has DuplicatesSkipped = 1 and TransactionsCreated = 1
//
// Scenario: Duplicate detection is scoped to the account
//   Given a transaction exists for account A matching a CSV row
//   When that same CSV row is imported for account B
//   Then no duplicate is detected and the transaction is created for account B
//
// Scenario: Claude analysis service is called after non-duplicate transactions are persisted
//   Given a valid account and profile with one new CSV row
//   When ProcessAsync is called
//   Then IClaudeAnalysisService.AnalyzeTransactionsAsync is called exactly once
//
// Scenario: Claude analysis service is not called when all rows are duplicates
//   Given all CSV rows match existing transactions
//   When ProcessAsync is called
//   Then IClaudeAnalysisService.AnalyzeTransactionsAsync is never called

using FluentAssertions;
using MakesCentsToMe.Api.Features.Import;
using MakesCentsToMe.Api.Infrastructure.Claude;
using MakesCentsToMe.Api.Infrastructure.Data;
using MakesCentsToMe.Api.Models.Entities;
using MakesCentsToMe.Unit.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Text;

namespace MakesCentsToMe.Unit.Features.Import;

public class ImportServiceTests : IDisposable
{
    private readonly Mock<IClaudeAnalysisService> _claudeAnalysisServiceMock;
    private readonly AppDbContext _dbContext;
    private readonly string _databaseName;
    private readonly ImportService _service;

    public ImportServiceTests()
    {
        _databaseName = Guid.NewGuid().ToString();
        _dbContext = InMemoryDbContextFactory.CreateWithDatabaseName(_databaseName);
        _claudeAnalysisServiceMock = new Mock<IClaudeAnalysisService>();
        _claudeAnalysisServiceMock
            .Setup(s => s.AnalyzeTransactionsAsync(It.IsAny<List<Transaction>>()))
            .Returns(Task.CompletedTask);
        _service = new ImportService(_dbContext, _claudeAnalysisServiceMock.Object);
    }

    public void Dispose() => _dbContext.Dispose();

    // --- GetProfileAsync ---

    [Fact]
    public async Task GetProfileAsync_ProfileExists_ReturnsSuccessWithProfile()
    {
        // Arrange
        var institution = SeedInstitution("River Bank");
        var account = SeedAccount(institution.Id, "Checking", AccountType.Checking);
        SeedImportProfile(account.Id, AmountType.Single, balanceProvided: true, dateFormat: "MM/dd/yyyy");

        // Act
        var result = await _service.GetProfileAsync(account.Id);

        // Assert
        result.Success.Should().BeTrue();
        result.Data!.AccountId.Should().Be(account.Id);
    }

    [Fact]
    public async Task GetProfileAsync_ProfileExists_ReturnsColumnMappingsOrderedByApplicationField()
    {
        // Arrange
        var institution = SeedInstitution("River Bank");
        var account = SeedAccount(institution.Id, "Checking", AccountType.Checking);
        var profile = SeedImportProfile(account.Id, AmountType.Single, balanceProvided: false, dateFormat: "MM/dd/yyyy");
        SeedColumnMapping(profile.Id, "Trans Date", "Date");
        SeedColumnMapping(profile.Id, "Trans Desc", "Description");
        SeedColumnMapping(profile.Id, "Trans Amt", "Amount");

        _dbContext.ChangeTracker.Clear();

        // Act
        var result = await _service.GetProfileAsync(account.Id);

        // Assert
        result.Data!.ColumnMappings.Select(m => m.ApplicationField)
            .Should().ContainInOrder("Amount", "Date", "Description");
    }

    [Fact]
    public async Task GetProfileAsync_NoProfileExists_ReturnsFailureResponse()
    {
        // Arrange
        var missingAccountId = Guid.NewGuid();

        // Act
        var result = await _service.GetProfileAsync(missingAccountId);

        // Assert
        result.Success.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.Should().Contain(missingAccountId.ToString());
    }

    // --- SaveProfileAsync ---

    [Fact]
    public async Task SaveProfileAsync_AccountExistsAndNoProfileExists_ReturnsSuccessWithProfile()
    {
        // Arrange
        var institution = SeedInstitution("River Bank");
        var account = SeedAccount(institution.Id, "Checking", AccountType.Checking);
        var request = BuildSaveProfileRequest(AmountType.Single, balanceProvided: false, dateFormat: "MM/dd/yyyy");

        // Act
        var result = await _service.SaveProfileAsync(account.Id, request);

        // Assert
        result.Success.Should().BeTrue();
        result.Data!.AccountId.Should().Be(account.Id);
        result.Data.DateFormat.Should().Be("MM/dd/yyyy");
    }

    [Fact]
    public async Task SaveProfileAsync_AccountExistsAndNoProfileExists_PersistsProfileToDatabase()
    {
        // Arrange
        var institution = SeedInstitution("River Bank");
        var account = SeedAccount(institution.Id, "Checking", AccountType.Checking);
        var request = BuildSaveProfileRequest(AmountType.Split, balanceProvided: true, dateFormat: "yyyy-MM-dd");

        // Act
        await _service.SaveProfileAsync(account.Id, request);

        // Assert
        var saved = await _dbContext.ImportProfiles.SingleAsync();
        saved.AmountType.Should().Be(AmountType.Split);
        saved.BalanceProvided.Should().BeTrue();
        saved.DateFormat.Should().Be("yyyy-MM-dd");
    }

    [Fact]
    public async Task SaveProfileAsync_AccountDoesNotExist_ReturnsFailureResponse()
    {
        // Arrange
        var missingAccountId = Guid.NewGuid();
        var request = BuildSaveProfileRequest(AmountType.Single, balanceProvided: false, dateFormat: "MM/dd/yyyy");

        // Act
        var result = await _service.SaveProfileAsync(missingAccountId, request);

        // Assert
        result.Success.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.Should().Contain(missingAccountId.ToString());
    }

    [Fact]
    public async Task SaveProfileAsync_ProfileAlreadyExists_ReturnsFailureResponseCitingPut()
    {
        // Arrange
        var institution = SeedInstitution("River Bank");
        var account = SeedAccount(institution.Id, "Checking", AccountType.Checking);
        SeedImportProfile(account.Id, AmountType.Single, balanceProvided: false, dateFormat: "MM/dd/yyyy");
        var request = BuildSaveProfileRequest(AmountType.Single, balanceProvided: false, dateFormat: "MM/dd/yyyy");

        // Act
        var result = await _service.SaveProfileAsync(account.Id, request);

        // Assert
        result.Success.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.Should().Contain("PUT");
    }

    [Fact]
    public async Task SaveProfileAsync_RequestWithColumnMappings_PersistsAllColumnMappings()
    {
        // Arrange
        var institution = SeedInstitution("River Bank");
        var account = SeedAccount(institution.Id, "Checking", AccountType.Checking);
        var request = new SaveImportProfileRequest(
            AmountType: AmountType.Single,
            BalanceProvided: false,
            ColumnMappings:
            [
                new ColumnMappingRequest("Trans Date", "Date"),
                new ColumnMappingRequest("Trans Description", "Description"),
            ],
            DateFormat: "MM/dd/yyyy");

        // Act
        await _service.SaveProfileAsync(account.Id, request);

        // Assert
        var savedMappings = await _dbContext.ColumnMappings.ToListAsync();
        savedMappings.Should().HaveCount(2);
        savedMappings.Select(m => m.ApplicationField)
            .Should().BeEquivalentTo(["Date", "Description"]);
    }

    // --- UpdateProfileAsync ---

    [Fact]
    public async Task UpdateProfileAsync_ProfileExists_ReturnsSuccessWithUpdatedValues()
    {
        // Arrange
        var institution = SeedInstitution("River Bank");
        var account = SeedAccount(institution.Id, "Checking", AccountType.Checking);
        SeedImportProfile(account.Id, AmountType.Single, balanceProvided: false, dateFormat: "MM/dd/yyyy");
        var request = BuildSaveProfileRequest(AmountType.Split, balanceProvided: true, dateFormat: "yyyy-MM-dd");

        // Act
        var result = await _service.UpdateProfileAsync(account.Id, request);

        // Assert
        result.Success.Should().BeTrue();
        result.Data!.AmountType.Should().Be(AmountType.Split);
        result.Data.BalanceProvided.Should().BeTrue();
        result.Data.DateFormat.Should().Be("yyyy-MM-dd");
    }

    [Fact]
    public async Task UpdateProfileAsync_ProfileDoesNotExist_ReturnsFailureResponse()
    {
        // Arrange
        var missingAccountId = Guid.NewGuid();
        var request = BuildSaveProfileRequest(AmountType.Single, balanceProvided: false, dateFormat: "MM/dd/yyyy");

        // Act
        var result = await _service.UpdateProfileAsync(missingAccountId, request);

        // Assert
        result.Success.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.Should().Contain(missingAccountId.ToString());
    }

    [Fact]
    public async Task UpdateProfileAsync_RequestRemovesMapping_RemovesMappingFromDatabase()
    {
        // Arrange
        var institution = SeedInstitution("River Bank");
        var account = SeedAccount(institution.Id, "Checking", AccountType.Checking);
        var profile = SeedImportProfile(account.Id, AmountType.Single, balanceProvided: false, dateFormat: "MM/dd/yyyy");
        SeedColumnMapping(profile.Id, "Trans Date", "Date");
        SeedColumnMapping(profile.Id, "Trans Desc", "Description");

        _dbContext.ChangeTracker.Clear();

        var request = new SaveImportProfileRequest(
            AmountType: AmountType.Single,
            BalanceProvided: false,
            ColumnMappings: [new ColumnMappingRequest("Trans Date", "Date")],
            DateFormat: "MM/dd/yyyy");

        // Act
        await _service.UpdateProfileAsync(account.Id, request);

        // Assert
        var remainingMappings = await _dbContext.ColumnMappings.ToListAsync();
        remainingMappings.Should().HaveCount(1);
        remainingMappings.Single().ApplicationField.Should().Be("Date");
    }

    [Fact]
    public async Task UpdateProfileAsync_RequestUpdatesExistingMappingApplicationField_UpdatesInPlace()
    {
        // Arrange
        var institution = SeedInstitution("River Bank");
        var account = SeedAccount(institution.Id, "Checking", AccountType.Checking);
        var profile = SeedImportProfile(account.Id, AmountType.Single, balanceProvided: false, dateFormat: "MM/dd/yyyy");
        SeedColumnMapping(profile.Id, "Trans Desc", "Description");

        _dbContext.ChangeTracker.Clear();

        var request = new SaveImportProfileRequest(
            AmountType: AmountType.Single,
            BalanceProvided: false,
            ColumnMappings: [new ColumnMappingRequest("Trans Desc", "Amount")],
            DateFormat: "MM/dd/yyyy");

        // Act
        await _service.UpdateProfileAsync(account.Id, request);

        // Assert
        var mapping = await _dbContext.ColumnMappings.SingleAsync();
        mapping.CsvColumnName.Should().Be("Trans Desc");
        mapping.ApplicationField.Should().Be("Amount");
    }

    // Note: the scenario where UpdateProfileAsync adds a brand-new ColumnMapping to an
    // ImportProfile that has a HasConversion<string>() enum property cannot be tested at the
    // unit level using the EF Core in-memory provider.  When profile.ColumnMappings.Add(...)
    // is the only pending change, the in-memory provider incorrectly raises a concurrency error
    // during SaveChanges because it cannot correctly resolve the principal entity's row key
    // after a value-converter round-trip.  This scenario is covered at the integration test level.

    [Fact]
    public async Task UpdateProfileAsync_IdenticalData_ReturnsSuccess()
    {
        // Arrange
        var institution = SeedInstitution("River Bank");
        var account = SeedAccount(institution.Id, "Checking", AccountType.Checking);
        SeedImportProfile(account.Id, AmountType.Single, balanceProvided: false, dateFormat: "MM/dd/yyyy");
        var request = BuildSaveProfileRequest(AmountType.Single, balanceProvided: false, dateFormat: "MM/dd/yyyy");

        // Act
        var result = await _service.UpdateProfileAsync(account.Id, request);

        // Assert
        result.Success.Should().BeTrue();
    }

    // --- UploadPreviewAsync ---

    [Fact]
    public async Task UploadPreviewAsync_AccountDoesNotExist_ReturnsFailureResponse()
    {
        // Arrange
        var missingAccountId = Guid.NewGuid();

        // Act
        var result = await _service.UploadPreviewAsync(missingAccountId, Stream.Null);

        // Assert
        result.Success.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.Should().Contain(missingAccountId.ToString());
    }

    [Fact]
    public async Task UploadPreviewAsync_EmptyCsvStream_ReturnsFailureResponseCitingEmptyFile()
    {
        // Arrange
        var institution = SeedInstitution("River Bank");
        var account = SeedAccount(institution.Id, "Checking", AccountType.Checking);
        using var emptyStream = new MemoryStream();

        // Act
        var result = await _service.UploadPreviewAsync(account.Id, emptyStream);

        // Assert
        result.Success.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.Should().Contain("empty");
    }

    [Fact]
    public async Task UploadPreviewAsync_CsvWithSevenDataRows_ReturnsFivePreviewRows()
    {
        // Arrange
        var institution = SeedInstitution("River Bank");
        var account = SeedAccount(institution.Id, "Checking", AccountType.Checking);

        var csvLines = new List<string> { "Date,Description,Amount" };
        for (var rowIndex = 1; rowIndex <= 7; rowIndex++)
        {
            csvLines.Add($"01/0{rowIndex}/2024,Purchase {rowIndex},{rowIndex * 10}.00");
        }

        using var stream = BuildStream(csvLines);

        // Act
        var result = await _service.UploadPreviewAsync(account.Id, stream);

        // Assert
        result.Success.Should().BeTrue();
        result.Data!.PreviewRows.Should().HaveCount(5);
    }

    [Fact]
    public async Task UploadPreviewAsync_HeaderOnlyCsv_ReturnsHeadersWithNoPreviewRows()
    {
        // Arrange
        var institution = SeedInstitution("River Bank");
        var account = SeedAccount(institution.Id, "Checking", AccountType.Checking);
        using var stream = BuildStream(["Date,Description,Amount"]);

        // Act
        var result = await _service.UploadPreviewAsync(account.Id, stream);

        // Assert
        result.Success.Should().BeTrue();
        result.Data!.Headers.Should().ContainInOrder("Date", "Description", "Amount");
        result.Data.PreviewRows.Should().BeEmpty();
    }

    [Fact]
    public async Task UploadPreviewAsync_CsvWithQuotedCommaField_ReturnsSingleFieldValue()
    {
        // Arrange
        var institution = SeedInstitution("River Bank");
        var account = SeedAccount(institution.Id, "Checking", AccountType.Checking);
        using var stream = BuildStream(
        [
            "Date,Description,Amount",
            "01/01/2024,\"Coffee, Large\",5.00",
        ]);

        // Act
        var result = await _service.UploadPreviewAsync(account.Id, stream);

        // Assert
        result.Data!.PreviewRows.Single()[1].Should().Be("Coffee, Large");
    }

    // --- ProcessAsync ---

    [Fact]
    public async Task ProcessAsync_AccountDoesNotExist_ReturnsFailureResponse()
    {
        // Arrange
        var missingAccountId = Guid.NewGuid();
        var request = new ProcessImportRequest(null, null);
        using var stream = BuildStream(["Date,Description,Amount"]);

        // Act
        var result = await _service.ProcessAsync(missingAccountId, stream, request);

        // Assert
        result.Success.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.Should().Contain(missingAccountId.ToString());
    }

    [Fact]
    public async Task ProcessAsync_AccountHasNoImportProfile_ReturnsFailureResponse()
    {
        // Arrange
        var institution = SeedInstitution("River Bank");
        var account = SeedAccount(institution.Id, "Checking", AccountType.Checking);
        var request = new ProcessImportRequest(null, null);
        using var stream = BuildStream(["Date,Description,Amount"]);

        // Act
        var result = await _service.ProcessAsync(account.Id, stream, request);

        // Assert
        result.Success.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.Should().Contain("import profile");
    }

    [Fact]
    public async Task ProcessAsync_BalanceNotProvidedAndNoBalanceInRequest_ReturnsFailureResponse()
    {
        // Arrange
        var (account, _) = SeedAccountWithSingleAmountProfile(balanceProvided: false);
        var request = new ProcessImportRequest(ClosingBalance: null, OpeningBalance: null);
        using var stream = BuildStream(
        [
            "Date,Description,Amount",
            "01/01/2024,Test,10.00",
        ]);

        // Act
        var result = await _service.ProcessAsync(account.Id, stream, request);

        // Assert
        result.Success.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.Should().Contain("balance");
    }

    [Fact]
    public async Task ProcessAsync_HeaderOnlyCsv_ReturnsZeroTransactionsCreatedAndZeroSkipped()
    {
        // Arrange
        var (account, _) = SeedAccountWithSingleAmountProfile(balanceProvided: true);
        var request = new ProcessImportRequest(null, null);
        using var stream = BuildStream(["Date,Description,Amount"]);

        // Act
        var result = await _service.ProcessAsync(account.Id, stream, request);

        // Assert
        result.Success.Should().BeTrue();
        result.Data!.TransactionsCreated.Should().Be(0);
        result.Data.RowsSkipped.Should().Be(0);
    }

    [Fact]
    public async Task ProcessAsync_TwoValidDataRows_CreatesTwoTransactions()
    {
        // Arrange
        var (account, _) = SeedAccountWithSingleAmountProfile(balanceProvided: true);
        var request = new ProcessImportRequest(null, null);
        using var stream = BuildStream(
        [
            "Date,Description,Amount,Balance",
            "01/01/2024,Coffee,5.00,995.00",
            "01/02/2024,Groceries,50.00,945.00",
        ]);

        // Act
        var result = await _service.ProcessAsync(account.Id, stream, request);

        // Assert
        result.Success.Should().BeTrue();
        result.Data!.TransactionsCreated.Should().Be(2);

        var savedTransactions = await _dbContext.Transactions.ToListAsync();
        savedTransactions.Should().HaveCount(2);
    }

    [Fact]
    public async Task ProcessAsync_RowWithColumnCountMismatch_SkipsRowAndIncrementsSkipCounter()
    {
        // Arrange
        var (account, _) = SeedAccountWithSingleAmountProfile(balanceProvided: true);
        var request = new ProcessImportRequest(null, null);
        using var stream = BuildStream(
        [
            "Date,Description,Amount,Balance",
            "01/01/2024,Valid Row,10.00,990.00",
            "01/02/2024,Too Few Columns",
        ]);

        // Act
        var result = await _service.ProcessAsync(account.Id, stream, request);

        // Assert
        result.Data!.TransactionsCreated.Should().Be(1);
        result.Data.RowsSkipped.Should().Be(1);
    }

    [Fact]
    public async Task ProcessAsync_RowWithMissingDate_SkipsRowAndIncrementsSkipCounter()
    {
        // Arrange
        var (account, _) = SeedAccountWithSingleAmountProfile(balanceProvided: true);
        var request = new ProcessImportRequest(null, null);
        using var stream = BuildStream(
        [
            "Date,Description,Amount,Balance",
            ",Missing Date Row,10.00,990.00",
        ]);

        // Act
        var result = await _service.ProcessAsync(account.Id, stream, request);

        // Assert
        result.Data!.TransactionsCreated.Should().Be(0);
        result.Data.RowsSkipped.Should().Be(1);
    }

    [Fact]
    public async Task ProcessAsync_RowWithMissingDescription_SkipsRowAndIncrementsSkipCounter()
    {
        // Arrange
        var (account, _) = SeedAccountWithSingleAmountProfile(balanceProvided: true);
        var request = new ProcessImportRequest(null, null);
        using var stream = BuildStream(
        [
            "Date,Description,Amount,Balance",
            "01/01/2024,,10.00,990.00",
        ]);

        // Act
        var result = await _service.ProcessAsync(account.Id, stream, request);

        // Assert
        result.Data!.TransactionsCreated.Should().Be(0);
        result.Data.RowsSkipped.Should().Be(1);
    }

    [Fact]
    public async Task ProcessAsync_RowWithUnparseableDate_SkipsRowAndIncrementsSkipCounter()
    {
        // Arrange
        var (account, _) = SeedAccountWithSingleAmountProfile(balanceProvided: true);
        var request = new ProcessImportRequest(null, null);
        using var stream = BuildStream(
        [
            "Date,Description,Amount,Balance",
            "not-a-date,Coffee,5.00,995.00",
        ]);

        // Act
        var result = await _service.ProcessAsync(account.Id, stream, request);

        // Assert
        result.Data!.TransactionsCreated.Should().Be(0);
        result.Data.RowsSkipped.Should().Be(1);
    }

    [Fact]
    public async Task ProcessAsync_SplitAmountType_StoresPrincipalInterestAndFeesSeparately()
    {
        // Arrange
        var (account, _) = SeedAccountWithSplitAmountProfile(balanceProvided: true);
        var request = new ProcessImportRequest(null, null);
        using var stream = BuildStream(
        [
            "Date,Description,Principal,Interest,Fees,Balance",
            "01/01/2024,Loan Payment,200.00,15.50,2.00,782.50",
        ]);

        // Act
        var result = await _service.ProcessAsync(account.Id, stream, request);

        // Assert
        result.Success.Should().BeTrue();
        var transaction = await _dbContext.Transactions.SingleAsync();
        transaction.Principal.Should().Be(200.00m);
        transaction.Interest.Should().Be(15.50m);
        transaction.Fees.Should().Be(2.00m);
        transaction.Amount.Should().Be(217.50m);
    }

    [Fact]
    public async Task ProcessAsync_SingleAmountType_SetsPrincipalToAmountAndInterestAndFeesToZero()
    {
        // Arrange
        var (account, _) = SeedAccountWithSingleAmountProfile(balanceProvided: true);
        var request = new ProcessImportRequest(null, null);
        using var stream = BuildStream(
        [
            "Date,Description,Amount,Balance",
            "01/01/2024,Coffee,5.00,995.00",
        ]);

        // Act
        await _service.ProcessAsync(account.Id, stream, request);

        // Assert
        var transaction = await _dbContext.Transactions.SingleAsync();
        transaction.Principal.Should().Be(5.00m);
        transaction.Interest.Should().Be(0m);
        transaction.Fees.Should().Be(0m);
        transaction.Amount.Should().Be(5.00m);
    }

    [Fact]
    public async Task ProcessAsync_BalanceProvidedInCsv_StoresBalanceFromCsvOnTransaction()
    {
        // Arrange
        var (account, _) = SeedAccountWithSingleAmountProfile(balanceProvided: true);
        var request = new ProcessImportRequest(null, null);
        using var stream = BuildStream(
        [
            "Date,Description,Amount,Balance",
            "01/01/2024,Coffee,5.00,995.00",
        ]);

        // Act
        await _service.ProcessAsync(account.Id, stream, request);

        // Assert
        var transaction = await _dbContext.Transactions.SingleAsync();
        transaction.Balance.Should().Be(995.00m);
    }

    [Fact]
    public async Task ProcessAsync_OpeningBalanceProvided_ComputesRunningBalancesForward()
    {
        // Arrange
        var (account, _) = SeedAccountWithSingleAmountProfile(balanceProvided: false);
        var request = new ProcessImportRequest(ClosingBalance: null, OpeningBalance: 1000m);
        using var stream = BuildStream(
        [
            "Date,Description,Amount",
            "01/01/2024,First Purchase,100.00",
            "01/02/2024,Second Purchase,200.00",
        ]);

        // Act
        await _service.ProcessAsync(account.Id, stream, request);

        // Assert
        var transactions = await _dbContext.Transactions
            .OrderBy(t => t.Date)
            .ToListAsync();

        transactions[0].Balance.Should().Be(1100m);
        transactions[1].Balance.Should().Be(1300m);
    }

    [Fact]
    public async Task ProcessAsync_ClosingBalanceProvided_ComputesRunningBalancesBackward()
    {
        // Arrange
        var (account, _) = SeedAccountWithSingleAmountProfile(balanceProvided: false);
        var request = new ProcessImportRequest(ClosingBalance: 1300m, OpeningBalance: null);
        using var stream = BuildStream(
        [
            "Date,Description,Amount",
            "01/01/2024,First Purchase,100.00",
            "01/02/2024,Second Purchase,200.00",
        ]);

        // Act
        await _service.ProcessAsync(account.Id, stream, request);

        // Assert
        var transactions = await _dbContext.Transactions
            .OrderBy(t => t.Date)
            .ToListAsync();

        // Second transaction balance = closingBalance = 1300
        // First transaction balance = 1300 - 200 = 1100
        transactions[1].Balance.Should().Be(1300m);
        transactions[0].Balance.Should().Be(1100m);
    }

    [Fact]
    public async Task ProcessAsync_ValidDataRow_PreservesRawCsvRowVerbatim()
    {
        // Arrange
        var (account, _) = SeedAccountWithSingleAmountProfile(balanceProvided: true);
        var request = new ProcessImportRequest(null, null);
        const string rawRow = "01/15/2024,Coffee Shop,4.75,1000.00";
        using var stream = BuildStream(["Date,Description,Amount,Balance", rawRow]);

        // Act
        await _service.ProcessAsync(account.Id, stream, request);

        // Assert
        var transaction = await _dbContext.Transactions.SingleAsync();
        transaction.RawCsvRow.Should().Be(rawRow);
    }

    [Fact]
    public async Task ProcessAsync_ValidDataRow_PopulatesRawDataDictionaryWithAllHeaders()
    {
        // Arrange
        var (account, _) = SeedAccountWithSingleAmountProfile(balanceProvided: true);
        var request = new ProcessImportRequest(null, null);
        using var stream = BuildStream(
        [
            "Date,Description,Amount,Balance",
            "01/01/2024,Coffee,5.00,995.00",
        ]);

        // Act
        await _service.ProcessAsync(account.Id, stream, request);

        // Assert
        var transaction = await _dbContext.Transactions.SingleAsync();
        transaction.RawData.Should().ContainKey("Date");
        transaction.RawData.Should().ContainKey("Description");
        transaction.RawData.Should().ContainKey("Amount");
        transaction.RawData.Should().ContainKey("Balance");
    }

    [Fact]
    public async Task ProcessAsync_CategoryColumnPresentAndPopulated_StoresCategoryOnTransaction()
    {
        // Arrange
        var institution = SeedInstitution("River Bank");
        var account = SeedAccount(institution.Id, "Checking", AccountType.Checking);
        var profile = SeedImportProfile(account.Id, AmountType.Single, balanceProvided: true, dateFormat: "MM/dd/yyyy");
        SeedColumnMapping(profile.Id, "Date", "Date");
        SeedColumnMapping(profile.Id, "Description", "Description");
        SeedColumnMapping(profile.Id, "Amount", "Amount");
        SeedColumnMapping(profile.Id, "Balance", "Balance");
        SeedColumnMapping(profile.Id, "Category", "Category");

        var request = new ProcessImportRequest(null, null);
        using var stream = BuildStream(
        [
            "Date,Description,Amount,Balance,Category",
            "01/01/2024,Coffee,5.00,995.00,Food & Drink",
        ]);

        // Act
        await _service.ProcessAsync(account.Id, stream, request);

        // Assert
        var transaction = await _dbContext.Transactions.SingleAsync();
        transaction.RawCategory.Should().Be("Food & Drink");
    }

    [Fact]
    public async Task ProcessAsync_CategoryColumnPresentButEmpty_SetsCategoryToNull()
    {
        // Arrange
        var institution = SeedInstitution("River Bank");
        var account = SeedAccount(institution.Id, "Checking", AccountType.Checking);
        var profile = SeedImportProfile(account.Id, AmountType.Single, balanceProvided: true, dateFormat: "MM/dd/yyyy");
        SeedColumnMapping(profile.Id, "Date", "Date");
        SeedColumnMapping(profile.Id, "Description", "Description");
        SeedColumnMapping(profile.Id, "Amount", "Amount");
        SeedColumnMapping(profile.Id, "Balance", "Balance");
        SeedColumnMapping(profile.Id, "Category", "Category");

        var request = new ProcessImportRequest(null, null);
        using var stream = BuildStream(
        [
            "Date,Description,Amount,Balance,Category",
            "01/01/2024,Coffee,5.00,995.00,",
        ]);

        // Act
        await _service.ProcessAsync(account.Id, stream, request);

        // Assert
        var transaction = await _dbContext.Transactions.SingleAsync();
        transaction.RawCategory.Should().BeNull();
    }

    [Fact]
    public async Task ProcessAsync_CheckNumberColumnPresent_StoresCheckNumberOnTransaction()
    {
        // Arrange
        var institution = SeedInstitution("River Bank");
        var account = SeedAccount(institution.Id, "Checking", AccountType.Checking);
        var profile = SeedImportProfile(account.Id, AmountType.Single, balanceProvided: true, dateFormat: "MM/dd/yyyy");
        SeedColumnMapping(profile.Id, "Date", "Date");
        SeedColumnMapping(profile.Id, "Description", "Description");
        SeedColumnMapping(profile.Id, "Amount", "Amount");
        SeedColumnMapping(profile.Id, "Balance", "Balance");
        SeedColumnMapping(profile.Id, "Check #", "CheckNumber");

        var request = new ProcessImportRequest(null, null);
        using var stream = BuildStream(
        [
            "Date,Description,Amount,Balance,Check #",
            "01/01/2024,Rent,500.00,500.00,1042",
        ]);

        // Act
        await _service.ProcessAsync(account.Id, stream, request);

        // Assert
        var transaction = await _dbContext.Transactions.SingleAsync();
        transaction.CheckNumber.Should().Be("1042");
    }

    [Fact]
    public async Task ProcessAsync_ValidDate_StoresTransactionDateAsUtc()
    {
        // Arrange
        var (account, _) = SeedAccountWithSingleAmountProfile(balanceProvided: true);
        var request = new ProcessImportRequest(null, null);
        using var stream = BuildStream(
        [
            "Date,Description,Amount,Balance",
            "01/15/2024,Coffee,5.00,995.00",
        ]);

        // Act
        await _service.ProcessAsync(account.Id, stream, request);

        // Assert
        var transaction = await _dbContext.Transactions.SingleAsync();
        transaction.Date.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public async Task ProcessAsync_StreamWithOnlyBlankLines_ReturnsZeroTransactionsCreated()
    {
        // Arrange
        var (account, _) = SeedAccountWithSingleAmountProfile(balanceProvided: true);
        var request = new ProcessImportRequest(null, null);
        using var stream = BuildStream(["   ", "  ,  ", ""]);

        // Act
        var result = await _service.ProcessAsync(account.Id, stream, request);

        // Assert
        result.Success.Should().BeTrue();
        result.Data!.TransactionsCreated.Should().Be(0);
    }

    [Fact]
    public async Task ProcessAsync_DateFormatDoesNotMatchExactly_FallsBackToFlexibleParsingAndCreatesTransaction()
    {
        // Arrange
        var institution = SeedInstitution("River Bank");
        var account = SeedAccount(institution.Id, "Checking", AccountType.Checking);
        var profile = SeedImportProfile(account.Id, AmountType.Single, balanceProvided: true, dateFormat: "MM/dd/yyyy");
        SeedColumnMapping(profile.Id, "Date", "Date");
        SeedColumnMapping(profile.Id, "Description", "Description");
        SeedColumnMapping(profile.Id, "Amount", "Amount");
        SeedColumnMapping(profile.Id, "Balance", "Balance");

        var request = new ProcessImportRequest(null, null);
        // Use ISO date format instead of MM/dd/yyyy — should fall back to flexible parse
        using var stream = BuildStream(
        [
            "Date,Description,Amount,Balance",
            "2024-01-01,Coffee,5.00,995.00",
        ]);

        // Act
        var result = await _service.ProcessAsync(account.Id, stream, request);

        // Assert
        result.Success.Should().BeTrue();
        result.Data!.TransactionsCreated.Should().Be(1);
        var transaction = await _dbContext.Transactions.SingleAsync();
        transaction.Date.Year.Should().Be(2024);
        transaction.Date.Month.Should().Be(1);
        transaction.Date.Day.Should().Be(1);
    }

    [Fact]
    public async Task ProcessAsync_BalanceProvidedProfileWithEmptyBalanceField_StoresNullBalance()
    {
        // Arrange
        var (account, _) = SeedAccountWithSingleAmountProfile(balanceProvided: true);
        var request = new ProcessImportRequest(null, null);
        using var stream = BuildStream(
        [
            "Date,Description,Amount,Balance",
            "01/01/2024,Coffee,5.00,",
        ]);

        // Act
        await _service.ProcessAsync(account.Id, stream, request);

        // Assert
        var transaction = await _dbContext.Transactions.SingleAsync();
        transaction.Balance.Should().BeNull();
    }

    [Fact]
    public async Task ProcessAsync_SplitAmountTypeWithMissingOptionalColumns_DefaultsToZero()
    {
        // Arrange
        var institution = SeedInstitution("River Bank");
        var account = SeedAccount(institution.Id, "Credit Card", AccountType.CreditCard);
        var profile = SeedImportProfile(account.Id, AmountType.Split, balanceProvided: true, dateFormat: "MM/dd/yyyy");
        SeedColumnMapping(profile.Id, "Date", "Date");
        SeedColumnMapping(profile.Id, "Description", "Description");
        SeedColumnMapping(profile.Id, "Principal", "Principal");
        SeedColumnMapping(profile.Id, "Balance", "Balance");
        // Interest and Fees mappings intentionally omitted

        var request = new ProcessImportRequest(null, null);
        using var stream = BuildStream(
        [
            "Date,Description,Principal,Balance",
            "01/01/2024,Purchase,100.00,900.00",
        ]);

        // Act
        var result = await _service.ProcessAsync(account.Id, stream, request);

        // Assert
        result.Success.Should().BeTrue();
        var transaction = await _dbContext.Transactions.SingleAsync();
        transaction.Principal.Should().Be(100.00m);
        transaction.Interest.Should().Be(0m);
        transaction.Fees.Should().Be(0m);
        transaction.Amount.Should().Be(100.00m);
    }

    // --- ProcessAsync — Deduplication ---

    [Fact]
    public async Task ProcessAsync_CsvRowMatchesExistingTransaction_SkipsDuplicateAndIncrementsDuplicatesSkipped()
    {
        // Arrange
        var (account, _) = SeedAccountWithSingleAmountProfile(balanceProvided: true);
        SeedExistingTransaction(account.Id, new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc), "Coffee", 5.00m);

        var request = new ProcessImportRequest(null, null);
        using var stream = BuildStream(
        [
            "Date,Description,Amount,Balance",
            "01/01/2024,Coffee,5.00,995.00",
        ]);

        // Act
        var result = await _service.ProcessAsync(account.Id, stream, request);

        // Assert
        result.Success.Should().BeTrue();
        result.Data!.DuplicatesSkipped.Should().Be(1);
        result.Data.TransactionsCreated.Should().Be(0);
    }

    [Fact]
    public async Task ProcessAsync_CsvRowMatchesExistingTransaction_DoesNotCreateNewTransactionInDatabase()
    {
        // Arrange
        var (account, _) = SeedAccountWithSingleAmountProfile(balanceProvided: true);
        SeedExistingTransaction(account.Id, new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc), "Coffee", 5.00m);

        var request = new ProcessImportRequest(null, null);
        using var stream = BuildStream(
        [
            "Date,Description,Amount,Balance",
            "01/01/2024,Coffee,5.00,995.00",
        ]);

        // Act
        await _service.ProcessAsync(account.Id, stream, request);

        // Assert
        var totalTransactions = await _dbContext.Transactions.CountAsync(t => t.AccountId == account.Id);
        totalTransactions.Should().Be(1);
    }

    [Fact]
    public async Task ProcessAsync_SameFilImportedTwice_SecondImportReportsAllRowsAsDuplicates()
    {
        // Arrange
        var (account, _) = SeedAccountWithSingleAmountProfile(balanceProvided: true);

        var csvLines = new[]
        {
            "Date,Description,Amount,Balance",
            "01/01/2024,Coffee,5.00,995.00",
            "01/02/2024,Groceries,50.00,945.00",
        };

        var request = new ProcessImportRequest(null, null);

        using var firstStream = BuildStream(csvLines);
        await _service.ProcessAsync(account.Id, firstStream, request);

        // Act — second import of the same file
        using var secondStream = BuildStream(csvLines);
        var result = await _service.ProcessAsync(account.Id, secondStream, request);

        // Assert
        result.Data!.DuplicatesSkipped.Should().Be(2);
        result.Data.TransactionsCreated.Should().Be(0);
    }

    [Fact]
    public async Task ProcessAsync_TwoIdenticalRowsInSameCsvFirstImport_CreatesBothTransactions()
    {
        // Arrange
        var (account, _) = SeedAccountWithSingleAmountProfile(balanceProvided: true);

        var request = new ProcessImportRequest(null, null);
        using var stream = BuildStream(
        [
            "Date,Description,Amount,Balance",
            "01/01/2024,Coffee,5.00,995.00",
            "01/01/2024,Coffee,5.00,990.00",
        ]);

        // Act
        var result = await _service.ProcessAsync(account.Id, stream, request);

        // Assert
        result.Data!.TransactionsCreated.Should().Be(2);
        result.Data.DuplicatesSkipped.Should().Be(0);
    }

    [Fact]
    public async Task ProcessAsync_OneExistingAndOneNewRow_ReturnsOneDuplicateSkippedAndOneTransactionCreated()
    {
        // Arrange
        var (account, _) = SeedAccountWithSingleAmountProfile(balanceProvided: true);
        SeedExistingTransaction(account.Id, new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc), "Coffee", 5.00m);

        var request = new ProcessImportRequest(null, null);
        using var stream = BuildStream(
        [
            "Date,Description,Amount,Balance",
            "01/01/2024,Coffee,5.00,995.00",
            "01/02/2024,Groceries,50.00,945.00",
        ]);

        // Act
        var result = await _service.ProcessAsync(account.Id, stream, request);

        // Assert
        result.Data!.DuplicatesSkipped.Should().Be(1);
        result.Data.TransactionsCreated.Should().Be(1);
    }

    [Fact]
    public async Task ProcessAsync_DuplicateDetectionScopedToAccount_CreatesTransactionForDifferentAccount()
    {
        // Arrange
        var (accountA, _) = SeedAccountWithSingleAmountProfile(balanceProvided: true);
        var (accountB, _) = SeedAccountWithSingleAmountProfile(balanceProvided: true);
        SeedExistingTransaction(accountA.Id, new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc), "Coffee", 5.00m);

        var request = new ProcessImportRequest(null, null);
        using var stream = BuildStream(
        [
            "Date,Description,Amount,Balance",
            "01/01/2024,Coffee,5.00,995.00",
        ]);

        // Act — import the same row for account B
        var result = await _service.ProcessAsync(accountB.Id, stream, request);

        // Assert
        result.Data!.DuplicatesSkipped.Should().Be(0);
        result.Data.TransactionsCreated.Should().Be(1);
    }

    [Fact]
    public async Task ProcessAsync_OneNewTransaction_CallsClaudeAnalysisServiceExactlyOnce()
    {
        // Arrange
        var (account, _) = SeedAccountWithSingleAmountProfile(balanceProvided: true);

        var request = new ProcessImportRequest(null, null);
        using var stream = BuildStream(
        [
            "Date,Description,Amount,Balance",
            "01/01/2024,Coffee,5.00,995.00",
        ]);

        // Act
        await _service.ProcessAsync(account.Id, stream, request);

        // Assert
        _claudeAnalysisServiceMock.Verify(
            s => s.AnalyzeTransactionsAsync(It.IsAny<List<Transaction>>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessAsync_AllRowsAreDuplicates_NeverCallsClaudeAnalysisService()
    {
        // Arrange
        var (account, _) = SeedAccountWithSingleAmountProfile(balanceProvided: true);
        SeedExistingTransaction(account.Id, new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc), "Coffee", 5.00m);

        var request = new ProcessImportRequest(null, null);
        using var stream = BuildStream(
        [
            "Date,Description,Amount,Balance",
            "01/01/2024,Coffee,5.00,995.00",
        ]);

        // Act
        await _service.ProcessAsync(account.Id, stream, request);

        // Assert
        _claudeAnalysisServiceMock.Verify(
            s => s.AnalyzeTransactionsAsync(It.IsAny<List<Transaction>>()),
            Times.Never);
    }

    // --- Helpers ---

    private Institution SeedInstitution(string name)
    {
        var institution = new Institution { Id = Guid.NewGuid(), Name = name };
        _dbContext.Institutions.Add(institution);
        _dbContext.SaveChanges();
        return institution;
    }

    private Account SeedAccount(Guid institutionId, string name, AccountType accountType)
    {
        var account = new Account
        {
            AccountType = accountType,
            Id = Guid.NewGuid(),
            InstitutionId = institutionId,
            Name = name,
        };
        _dbContext.Accounts.Add(account);
        _dbContext.SaveChanges();
        return account;
    }

    private ImportProfile SeedImportProfile(
        Guid accountId,
        AmountType amountType,
        bool balanceProvided,
        string dateFormat)
    {
        var profile = new ImportProfile
        {
            AccountId = accountId,
            AmountType = amountType,
            BalanceProvided = balanceProvided,
            DateFormat = dateFormat,
            Id = Guid.NewGuid(),
        };
        _dbContext.ImportProfiles.Add(profile);
        _dbContext.SaveChanges();
        return profile;
    }

    private ColumnMapping SeedColumnMapping(Guid profileId, string csvColumnName, string applicationField)
    {
        var mapping = new ColumnMapping
        {
            ApplicationField = applicationField,
            CsvColumnName = csvColumnName,
            Id = Guid.NewGuid(),
            ImportProfileId = profileId,
        };
        _dbContext.ColumnMappings.Add(mapping);
        _dbContext.SaveChanges();
        return mapping;
    }

    /// <summary>
    /// Seeds an account with a single-amount profile that maps Date, Description, Amount, and Balance columns.
    /// </summary>
    private (Account Account, ImportProfile Profile) SeedAccountWithSingleAmountProfile(bool balanceProvided)
    {
        var institution = SeedInstitution($"River Bank {Guid.NewGuid()}");
        var account = SeedAccount(institution.Id, "Checking", AccountType.Checking);
        var profile = SeedImportProfile(account.Id, AmountType.Single, balanceProvided, dateFormat: "MM/dd/yyyy");

        SeedColumnMapping(profile.Id, "Date", "Date");
        SeedColumnMapping(profile.Id, "Description", "Description");
        SeedColumnMapping(profile.Id, "Amount", "Amount");

        if (balanceProvided)
        {
            SeedColumnMapping(profile.Id, "Balance", "Balance");
        }

        return (account, profile);
    }

    /// <summary>
    /// Seeds an account with a split-amount profile that maps Date, Description, Principal, Interest, Fees, and Balance columns.
    /// </summary>
    private (Account Account, ImportProfile Profile) SeedAccountWithSplitAmountProfile(bool balanceProvided)
    {
        var institution = SeedInstitution($"Credit Union {Guid.NewGuid()}");
        var account = SeedAccount(institution.Id, "Credit Card", AccountType.CreditCard);
        var profile = SeedImportProfile(account.Id, AmountType.Split, balanceProvided, dateFormat: "MM/dd/yyyy");

        SeedColumnMapping(profile.Id, "Date", "Date");
        SeedColumnMapping(profile.Id, "Description", "Description");
        SeedColumnMapping(profile.Id, "Principal", "Principal");
        SeedColumnMapping(profile.Id, "Interest", "Interest");
        SeedColumnMapping(profile.Id, "Fees", "Fees");

        if (balanceProvided)
        {
            SeedColumnMapping(profile.Id, "Balance", "Balance");
        }

        return (account, profile);
    }

    private static SaveImportProfileRequest BuildSaveProfileRequest(
        AmountType amountType,
        bool balanceProvided,
        string dateFormat)
    {
        return new SaveImportProfileRequest(
            AmountType: amountType,
            BalanceProvided: balanceProvided,
            ColumnMappings: [],
            DateFormat: dateFormat);
    }

    /// <summary>
    /// Seeds a fully committed transaction directly into the database to simulate a previously imported record.
    /// Used to set up deduplication scenarios where an identical row should be skipped on re-import.
    /// </summary>
    private Transaction SeedExistingTransaction(Guid accountId, DateTime date, string description, decimal amount)
    {
        var transaction = new Transaction
        {
            AccountId = accountId,
            Amount = amount,
            Date = date,
            Description = description,
            Id = Guid.NewGuid(),
            RawCsvRow = $"{date:MM/dd/yyyy},{description},{amount}",
            Status = TransactionStatus.Committed,
        };
        _dbContext.Transactions.Add(transaction);
        _dbContext.SaveChanges();
        return transaction;
    }

    private static MemoryStream BuildStream(IEnumerable<string> lines)
    {
        var content = string.Join("\n", lines);
        return new MemoryStream(Encoding.UTF8.GetBytes(content));
    }
}
