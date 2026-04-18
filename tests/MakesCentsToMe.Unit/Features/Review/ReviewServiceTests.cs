// Feature: Review Queue — Accept
//
// Scenario: Accept a pending review transaction
//   Given a transaction in PendingReview status with a suggested category and vendor
//   When AcceptAsync is called
//   Then the transaction status becomes Committed
//   And the CategoryId and NormalizedVendor are promoted from the suggested values
//
// Scenario: Accept promotes suggested category id to committed category id
//   Given a transaction in PendingReview status with SuggestedCategoryId set
//   When AcceptAsync is called
//   Then the transaction CategoryId matches the SuggestedCategoryId
//
// Scenario: Accept promotes suggested normalized vendor
//   Given a transaction in PendingReview status with SuggestedNormalizedVendor set
//   When AcceptAsync is called
//   Then the transaction NormalizedVendor matches the SuggestedNormalizedVendor
//
// Scenario: Accept when transaction does not exist
//   Given no transaction exists with the requested id
//   When AcceptAsync is called
//   Then a failure response is returned citing the missing id
//
// Scenario: Accept when transaction is already Committed
//   Given a transaction in Committed status
//   When AcceptAsync is called
//   Then a failure response is returned citing the wrong status
//
// Scenario: Accept when transaction is in Pending status
//   Given a transaction in Pending status
//   When AcceptAsync is called
//   Then a failure response is returned citing the wrong status
//
// Feature: Review Queue — AcceptAll
//
// Scenario: AcceptAll without account filter commits all pending review transactions
//   Given three transactions in PendingReview status across two accounts
//   When AcceptAllAsync is called without an accountId filter
//   Then all three transactions are committed and the count is returned
//
// Scenario: AcceptAll with account filter commits only transactions for that account
//   Given two transactions in PendingReview status in account A and one in account B
//   When AcceptAllAsync is called with account A's id
//   Then only the two transactions for account A are committed
//
// Scenario: AcceptAll does not affect transactions in PendingAnalysis status
//   Given one transaction in PendingReview and one in PendingAnalysis
//   When AcceptAllAsync is called without a filter
//   Then only the PendingReview transaction is committed
//
// Scenario: AcceptAll promotes suggested category and vendor for each committed transaction
//   Given a PendingReview transaction with SuggestedCategoryId and SuggestedNormalizedVendor set
//   When AcceptAllAsync is called
//   Then the transaction CategoryId and NormalizedVendor reflect the suggested values
//
// Scenario: AcceptAll returns zero when no pending review transactions exist
//   Given no transactions in PendingReview status
//   When AcceptAllAsync is called
//   Then the response data is zero
//
// Feature: Review Queue — ListPending
//
// Scenario: ListPending returns transactions in PendingReview and PendingAnalysis status
//   Given transactions in all four statuses
//   When ListPendingAsync is called without a filter
//   Then only PendingReview and PendingAnalysis transactions are returned
//
// Scenario: ListPending with account filter returns only transactions for that account
//   Given PendingReview transactions in two different accounts
//   When ListPendingAsync is called with one account's id
//   Then only that account's transactions are returned
//
// Scenario: ListPending returns results ordered by date descending then description ascending
//   Given three PendingReview transactions with different dates
//   When ListPendingAsync is called
//   Then results are ordered by date descending
//
// Scenario: ListPending returns empty list when no pending transactions exist
//   Given no transactions in PendingReview or PendingAnalysis status
//   When ListPendingAsync is called
//   Then an empty list is returned with a success response
//
// Scenario: ListPending maps account name and institution name correctly
//   Given a PendingReview transaction associated with an account and institution
//   When ListPendingAsync is called
//   Then the response includes the correct AccountName and InstitutionName
//
// Feature: Review Queue — Override
//
// Scenario: Override a pending review transaction with a new vendor and category
//   Given a transaction in PendingReview status
//   When OverrideAsync is called with a valid category id and normalized vendor
//   Then the transaction status becomes Committed
//   And the CategoryId and NormalizedVendor reflect the override values
//
// Scenario: Override stores the overridden normalized vendor
//   Given a transaction in PendingReview status
//   When OverrideAsync is called with NormalizedVendor "Starbucks Coffee"
//   Then the transaction NormalizedVendor is "Starbucks Coffee"
//
// Scenario: Override stores the overridden category id
//   Given a transaction in PendingReview status and a known category
//   When OverrideAsync is called with that category id
//   Then the transaction CategoryId matches the provided category id
//
// Scenario: Override when transaction does not exist
//   Given no transaction exists with the requested id
//   When OverrideAsync is called
//   Then a failure response is returned citing the missing id
//
// Scenario: Override when transaction is already Committed
//   Given a transaction in Committed status
//   When OverrideAsync is called
//   Then a failure response is returned citing the wrong status
//
// Scenario: Override with a CategoryId that does not exist
//   Given a transaction in PendingReview status
//   When OverrideAsync is called with a CategoryId that does not exist in the database
//   Then a failure response is returned citing the missing category
//
// Scenario: Override with null CategoryId succeeds
//   Given a transaction in PendingReview status
//   When OverrideAsync is called with CategoryId = null
//   Then the transaction is committed with a null CategoryId

using FluentAssertions;
using MakesCentsToMe.Api.Features.Review;
using MakesCentsToMe.Api.Infrastructure.Data;
using MakesCentsToMe.Api.Models.Entities;
using MakesCentsToMe.Unit.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace MakesCentsToMe.Unit.Features.Review;

public class ReviewServiceTests : IDisposable
{
    private readonly AppDbContext _dbContext;
    private readonly ReviewService _service;

    public ReviewServiceTests()
    {
        _dbContext = InMemoryDbContextFactory.Create();
        _service = new ReviewService(_dbContext);
    }

    public void Dispose() => _dbContext.Dispose();

    // --- AcceptAsync ---

    [Fact]
    public async Task AcceptAsync_PendingReviewTransaction_ReturnsSuccessWithCommittedStatus()
    {
        // Arrange
        var (_, transaction) = SeedTransactionInStatus(TransactionStatus.PendingReview);

        // Act
        var result = await _service.AcceptAsync(transaction.Id);

        // Assert
        result.Success.Should().BeTrue();
        result.Data!.Status.Should().Be("Committed");
    }

    [Fact]
    public async Task AcceptAsync_PendingReviewTransaction_PromotesSuggestedCategoryId()
    {
        // Arrange
        var category = SeedCategory("Groceries");
        var (_, transaction) = SeedTransactionInStatus(
            TransactionStatus.PendingReview,
            suggestedCategoryId: category.Id);

        // Act
        await _service.AcceptAsync(transaction.Id);

        // Assert
        var saved = await _dbContext.Transactions.FindAsync(transaction.Id);
        saved!.CategoryId.Should().Be(category.Id);
    }

    [Fact]
    public async Task AcceptAsync_PendingReviewTransaction_PromotesSuggestedNormalizedVendor()
    {
        // Arrange
        var (_, transaction) = SeedTransactionInStatus(
            TransactionStatus.PendingReview,
            suggestedNormalizedVendor: "Whole Foods Market");

        // Act
        await _service.AcceptAsync(transaction.Id);

        // Assert
        var saved = await _dbContext.Transactions.FindAsync(transaction.Id);
        saved!.NormalizedVendor.Should().Be("Whole Foods Market");
    }

    [Fact]
    public async Task AcceptAsync_PendingReviewTransaction_PersistsCommittedStatusToDatabase()
    {
        // Arrange
        var (_, transaction) = SeedTransactionInStatus(TransactionStatus.PendingReview);

        // Act
        await _service.AcceptAsync(transaction.Id);

        // Assert
        var saved = await _dbContext.Transactions.FindAsync(transaction.Id);
        saved!.Status.Should().Be(TransactionStatus.Committed);
    }

    [Fact]
    public async Task AcceptAsync_TransactionDoesNotExist_ReturnsFailureResponseCitingMissingId()
    {
        // Arrange
        var missingId = Guid.NewGuid();

        // Act
        var result = await _service.AcceptAsync(missingId);

        // Assert
        result.Success.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.Should().Contain(missingId.ToString());
    }

    [Fact]
    public async Task AcceptAsync_TransactionAlreadyCommitted_ReturnsFailureResponseCitingWrongStatus()
    {
        // Arrange
        var (_, transaction) = SeedTransactionInStatus(TransactionStatus.Committed);

        // Act
        var result = await _service.AcceptAsync(transaction.Id);

        // Assert
        result.Success.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.Should().Contain("Committed");
    }

    [Fact]
    public async Task AcceptAsync_TransactionInPendingStatus_ReturnsFailureResponseCitingWrongStatus()
    {
        // Arrange
        var (_, transaction) = SeedTransactionInStatus(TransactionStatus.Pending);

        // Act
        var result = await _service.AcceptAsync(transaction.Id);

        // Assert
        result.Success.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.Should().Contain("Pending");
    }

    // --- AcceptAllAsync ---

    [Fact]
    public async Task AcceptAllAsync_NoAccountFilter_CommitsAllPendingReviewTransactions()
    {
        // Arrange
        SeedTransactionInStatus(TransactionStatus.PendingReview);
        SeedTransactionInStatus(TransactionStatus.PendingReview);
        SeedTransactionInStatus(TransactionStatus.PendingReview);

        // Act
        var result = await _service.AcceptAllAsync(accountId: null);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().Be(3);

        var committed = await _dbContext.Transactions
            .CountAsync(t => t.Status == TransactionStatus.Committed);
        committed.Should().Be(3);
    }

    [Fact]
    public async Task AcceptAllAsync_WithAccountFilter_CommitsOnlyTransactionsForThatAccount()
    {
        // Arrange
        var (accountA, _) = SeedTransactionInStatus(TransactionStatus.PendingReview);
        SeedTransactionInStatus(TransactionStatus.PendingReview, accountId: accountA.Id);
        SeedTransactionInStatus(TransactionStatus.PendingReview); // different account

        // Act
        var result = await _service.AcceptAllAsync(accountId: accountA.Id);

        // Assert
        result.Data.Should().Be(2);

        var committedForAccountA = await _dbContext.Transactions
            .CountAsync(t => t.AccountId == accountA.Id && t.Status == TransactionStatus.Committed);
        committedForAccountA.Should().Be(2);
    }

    [Fact]
    public async Task AcceptAllAsync_NoAccountFilter_DoesNotAffectPendingAnalysisTransactions()
    {
        // Arrange
        SeedTransactionInStatus(TransactionStatus.PendingReview);
        SeedTransactionInStatus(TransactionStatus.PendingAnalysis);

        // Act
        await _service.AcceptAllAsync(accountId: null);

        // Assert
        var pendingAnalysisStillPending = await _dbContext.Transactions
            .CountAsync(t => t.Status == TransactionStatus.PendingAnalysis);
        pendingAnalysisStillPending.Should().Be(1);
    }

    [Fact]
    public async Task AcceptAllAsync_PendingReviewTransactionWithSuggestedValues_PromotesSuggestedValuesToCommitted()
    {
        // Arrange
        var category = SeedCategory("Dining");
        var (_, transaction) = SeedTransactionInStatus(
            TransactionStatus.PendingReview,
            suggestedCategoryId: category.Id,
            suggestedNormalizedVendor: "Chipotle Mexican Grill");

        // Act
        await _service.AcceptAllAsync(accountId: null);

        // Assert
        var saved = await _dbContext.Transactions.FindAsync(transaction.Id);
        saved!.CategoryId.Should().Be(category.Id);
        saved.NormalizedVendor.Should().Be("Chipotle Mexican Grill");
    }

    [Fact]
    public async Task AcceptAllAsync_NoPendingReviewTransactionsExist_ReturnsZero()
    {
        // Arrange
        SeedTransactionInStatus(TransactionStatus.Committed);

        // Act
        var result = await _service.AcceptAllAsync(accountId: null);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().Be(0);
    }

    // --- ListPendingAsync ---

    [Fact]
    public async Task ListPendingAsync_NoFilter_ReturnsPendingReviewAndPendingAnalysisTransactions()
    {
        // Arrange
        SeedTransactionInStatus(TransactionStatus.PendingReview);
        SeedTransactionInStatus(TransactionStatus.PendingAnalysis);
        SeedTransactionInStatus(TransactionStatus.Committed);
        SeedTransactionInStatus(TransactionStatus.Pending);

        // Act
        var result = await _service.ListPendingAsync(accountId: null);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(2);
        result.Data!.Select(t => t.Status)
            .Should().BeEquivalentTo(["PendingReview", "PendingAnalysis"]);
    }

    [Fact]
    public async Task ListPendingAsync_WithAccountFilter_ReturnsOnlyTransactionsForThatAccount()
    {
        // Arrange
        var (accountA, _) = SeedTransactionInStatus(TransactionStatus.PendingReview);
        SeedTransactionInStatus(TransactionStatus.PendingReview); // different account

        // Act
        var result = await _service.ListPendingAsync(accountId: accountA.Id);

        // Assert
        result.Data.Should().HaveCount(1);
        result.Data!.Single().AccountName.Should().Be(accountA.Name);
    }

    [Fact]
    public async Task ListPendingAsync_MultipleTransactions_ReturnsResultsOrderedByDateDescending()
    {
        // Arrange
        var (account, _) = SeedTransactionInStatus(
            TransactionStatus.PendingReview,
            date: new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        SeedTransactionInStatus(
            TransactionStatus.PendingReview,
            accountId: account.Id,
            date: new DateTime(2024, 3, 1, 0, 0, 0, DateTimeKind.Utc));
        SeedTransactionInStatus(
            TransactionStatus.PendingReview,
            accountId: account.Id,
            date: new DateTime(2024, 2, 1, 0, 0, 0, DateTimeKind.Utc));

        // Act
        var result = await _service.ListPendingAsync(accountId: null);

        // Assert
        result.Data!.Select(t => t.Date)
            .Should().BeInDescendingOrder();
    }

    [Fact]
    public async Task ListPendingAsync_NoPendingTransactionsExist_ReturnsSuccessWithEmptyList()
    {
        // Arrange
        SeedTransactionInStatus(TransactionStatus.Committed);

        // Act
        var result = await _service.ListPendingAsync(accountId: null);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task ListPendingAsync_PendingReviewTransaction_MapsAccountNameAndInstitutionNameCorrectly()
    {
        // Arrange
        var institution = SeedInstitution("First National Bank");
        var account = SeedAccount(institution.Id, "Checking Account");
        SeedTransaction(account.Id, TransactionStatus.PendingReview);

        // Act
        var result = await _service.ListPendingAsync(accountId: null);

        // Assert
        var response = result.Data!.Single();
        response.AccountName.Should().Be("Checking Account");
        response.InstitutionName.Should().Be("First National Bank");
    }

    // --- OverrideAsync ---

    [Fact]
    public async Task OverrideAsync_PendingReviewTransaction_ReturnsSuccessWithCommittedStatus()
    {
        // Arrange
        var category = SeedCategory("Transportation");
        var (_, transaction) = SeedTransactionInStatus(TransactionStatus.PendingReview);
        var request = new OverrideTransactionRequest(CategoryId: category.Id, NormalizedVendor: "Uber");

        // Act
        var result = await _service.OverrideAsync(transaction.Id, request);

        // Assert
        result.Success.Should().BeTrue();
        result.Data!.Status.Should().Be("Committed");
    }

    [Fact]
    public async Task OverrideAsync_PendingReviewTransaction_StoresOverriddenNormalizedVendor()
    {
        // Arrange
        var (_, transaction) = SeedTransactionInStatus(TransactionStatus.PendingReview);
        var request = new OverrideTransactionRequest(CategoryId: null, NormalizedVendor: "Starbucks Coffee");

        // Act
        await _service.OverrideAsync(transaction.Id, request);

        // Assert
        var saved = await _dbContext.Transactions.FindAsync(transaction.Id);
        saved!.NormalizedVendor.Should().Be("Starbucks Coffee");
    }

    [Fact]
    public async Task OverrideAsync_PendingReviewTransaction_StoresOverriddenCategoryId()
    {
        // Arrange
        var category = SeedCategory("Dining");
        var (_, transaction) = SeedTransactionInStatus(TransactionStatus.PendingReview);
        var request = new OverrideTransactionRequest(CategoryId: category.Id, NormalizedVendor: "Chipotle");

        // Act
        await _service.OverrideAsync(transaction.Id, request);

        // Assert
        var saved = await _dbContext.Transactions.FindAsync(transaction.Id);
        saved!.CategoryId.Should().Be(category.Id);
    }

    [Fact]
    public async Task OverrideAsync_PendingReviewTransaction_PersistsCommittedStatusToDatabase()
    {
        // Arrange
        var (_, transaction) = SeedTransactionInStatus(TransactionStatus.PendingReview);
        var request = new OverrideTransactionRequest(CategoryId: null, NormalizedVendor: "Amazon");

        // Act
        await _service.OverrideAsync(transaction.Id, request);

        // Assert
        var saved = await _dbContext.Transactions.FindAsync(transaction.Id);
        saved!.Status.Should().Be(TransactionStatus.Committed);
    }

    [Fact]
    public async Task OverrideAsync_TransactionDoesNotExist_ReturnsFailureResponseCitingMissingId()
    {
        // Arrange
        var missingId = Guid.NewGuid();
        var request = new OverrideTransactionRequest(CategoryId: null, NormalizedVendor: "Any Vendor");

        // Act
        var result = await _service.OverrideAsync(missingId, request);

        // Assert
        result.Success.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.Should().Contain(missingId.ToString());
    }

    [Fact]
    public async Task OverrideAsync_TransactionAlreadyCommitted_ReturnsFailureResponseCitingWrongStatus()
    {
        // Arrange
        var (_, transaction) = SeedTransactionInStatus(TransactionStatus.Committed);
        var request = new OverrideTransactionRequest(CategoryId: null, NormalizedVendor: "Any Vendor");

        // Act
        var result = await _service.OverrideAsync(transaction.Id, request);

        // Assert
        result.Success.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.Should().Contain("Committed");
    }

    [Fact]
    public async Task OverrideAsync_CategoryIdDoesNotExist_ReturnsFailureResponseCitingMissingCategory()
    {
        // Arrange
        var (_, transaction) = SeedTransactionInStatus(TransactionStatus.PendingReview);
        var missingCategoryId = Guid.NewGuid();
        var request = new OverrideTransactionRequest(CategoryId: missingCategoryId, NormalizedVendor: "Vendor");

        // Act
        var result = await _service.OverrideAsync(transaction.Id, request);

        // Assert
        result.Success.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.Should().Contain(missingCategoryId.ToString());
    }

    [Fact]
    public async Task OverrideAsync_NullCategoryId_ReturnsSuccessAndCommitsTransactionWithNullCategoryId()
    {
        // Arrange
        var (_, transaction) = SeedTransactionInStatus(TransactionStatus.PendingReview);
        var request = new OverrideTransactionRequest(CategoryId: null, NormalizedVendor: "Amazon");

        // Act
        var result = await _service.OverrideAsync(transaction.Id, request);

        // Assert
        result.Success.Should().BeTrue();
        var saved = await _dbContext.Transactions.FindAsync(transaction.Id);
        saved!.CategoryId.Should().BeNull();
    }

    // --- Helpers ---

    private Category SeedCategory(string name)
    {
        var category = new Category
        {
            Id = Guid.NewGuid(),
            IsDefault = false,
            Name = name,
        };
        _dbContext.Categories.Add(category);
        _dbContext.SaveChanges();
        return category;
    }

    private Institution SeedInstitution(string name)
    {
        var institution = new Institution { Id = Guid.NewGuid(), Name = name };
        _dbContext.Institutions.Add(institution);
        _dbContext.SaveChanges();
        return institution;
    }

    private Account SeedAccount(Guid institutionId, string name)
    {
        var account = new Account
        {
            AccountType = AccountType.Checking,
            Id = Guid.NewGuid(),
            InstitutionId = institutionId,
            Name = name,
        };
        _dbContext.Accounts.Add(account);
        _dbContext.SaveChanges();
        return account;
    }

    private Transaction SeedTransaction(
        Guid accountId,
        TransactionStatus status,
        DateTime? date = null,
        Guid? suggestedCategoryId = null,
        string? suggestedNormalizedVendor = null)
    {
        var transaction = new Transaction
        {
            AccountId = accountId,
            Amount = 25.00m,
            Date = date ?? new DateTime(2024, 6, 15, 0, 0, 0, DateTimeKind.Utc),
            Description = "Test Purchase",
            Id = Guid.NewGuid(),
            RawCsvRow = "2024-06-15,Test Purchase,25.00",
            Status = status,
            SuggestedCategoryId = suggestedCategoryId,
            SuggestedNormalizedVendor = suggestedNormalizedVendor,
        };
        _dbContext.Transactions.Add(transaction);
        _dbContext.SaveChanges();
        return transaction;
    }

    /// <summary>
    /// Seeds an institution, account, and transaction in the given status.
    /// Returns the account and transaction for use in assertions.
    /// When accountId is provided, the transaction is added to that existing account instead.
    /// </summary>
    private (Account Account, Transaction Transaction) SeedTransactionInStatus(
        TransactionStatus status,
        Guid? accountId = null,
        DateTime? date = null,
        Guid? suggestedCategoryId = null,
        string? suggestedNormalizedVendor = null)
    {
        Account account;

        if (accountId.HasValue)
        {
            account = _dbContext.Accounts.Find(accountId.Value)!;
        }
        else
        {
            var institution = SeedInstitution($"Test Bank {Guid.NewGuid()}");
            account = SeedAccount(institution.Id, $"Account {Guid.NewGuid()}");
        }

        var transaction = SeedTransaction(
            account.Id,
            status,
            date: date,
            suggestedCategoryId: suggestedCategoryId,
            suggestedNormalizedVendor: suggestedNormalizedVendor);

        return (account, transaction);
    }
}
