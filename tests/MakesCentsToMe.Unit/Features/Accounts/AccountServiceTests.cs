// Feature: Account Management
//
// Scenario: Create account under a valid institution
//   Given an institution exists
//   When CreateAsync is called with a valid request
//   Then the account is persisted and returned in the response
//
// Scenario: Create account when institution does not exist
//   Given no institution exists with the given id
//   When CreateAsync is called
//   Then a failure response is returned
//
// Scenario: Create account with duplicate name in same institution
//   Given an account named "Checking" already exists in the institution
//   When CreateAsync is called with the same name
//   Then a failure response is returned citing the duplicate name
//
// Scenario: Create account with same name in different institution is allowed
//   Given an account named "Checking" exists in institution A
//   When CreateAsync is called with name "Checking" for institution B
//   Then the account is created successfully
//
// Scenario: Create account reflects no import profile initially
//   Given an institution exists
//   When CreateAsync is called
//   Then the returned account has HasImportProfile = false
//
// Scenario: Get account by id when account exists
//   Given an account exists under the institution
//   When GetByIdAsync is called with matching institutionId and accountId
//   Then the account is returned
//
// Scenario: Get account by id when account belongs to different institution
//   Given an account exists under institution A
//   When GetByIdAsync is called with institution B's id
//   Then a failure response is returned
//
// Scenario: Get account by id when account does not exist
//   Given no account exists with the requested id
//   When GetByIdAsync is called
//   Then a failure response is returned
//
// Scenario: Get account reflects import profile presence
//   Given an account with an import profile exists
//   When GetByIdAsync is called
//   Then the response has HasImportProfile = true
//
// Scenario: List accounts when institution does not exist
//   Given no institution exists with the given id
//   When ListAsync is called
//   Then a failure response is returned
//
// Scenario: List accounts returns all accounts for the institution ordered by name
//   Given multiple accounts exist under an institution
//   When ListAsync is called
//   Then all accounts are returned ordered alphabetically by name
//
// Scenario: List accounts does not return accounts from another institution
//   Given accounts exist under two different institutions
//   When ListAsync is called for institution A
//   Then only institution A's accounts are returned
//
// Scenario: Update account name when account exists
//   Given an account exists
//   When UpdateAsync is called with a new name
//   Then the name is updated and the updated data is returned
//
// Scenario: Update account type when account exists
//   Given an account exists with type Checking
//   When UpdateAsync is called with type CreditCard
//   Then the account type is updated
//
// Scenario: Update account when account does not exist
//   Given no account exists with the requested id
//   When UpdateAsync is called
//   Then a failure response is returned
//
// Scenario: Update account with duplicate name in same institution
//   Given two accounts "Alpha" and "Beta" exist in the institution
//   When UpdateAsync renames "Alpha" to "Beta"
//   Then a failure response is returned citing the duplicate name
//
// Scenario: Update account name to its own current name is allowed
//   Given an account named "Checking" exists
//   When UpdateAsync is called with the same name "Checking"
//   Then the response is successful
//
// Scenario: Delete account with no transactions
//   Given an account with no transactions exists
//   When DeleteAsync is called
//   Then the account is removed and a success response is returned
//
// Scenario: Delete account that has transactions
//   Given an account with transactions exists
//   When DeleteAsync is called
//   Then a failure response is returned and the account is not deleted
//
// Scenario: Delete account when account does not exist
//   Given no account exists with the requested id
//   When DeleteAsync is called
//   Then a failure response is returned
//
// Scenario: Delete account scoped to institution — wrong institution returns not found
//   Given an account exists under institution A
//   When DeleteAsync is called with institution B's id
//   Then a failure response is returned

using FluentAssertions;
using MakesCentsToMe.Api.Features.Accounts;
using MakesCentsToMe.Api.Infrastructure.Data;
using MakesCentsToMe.Api.Models.Entities;
using MakesCentsToMe.Unit.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace MakesCentsToMe.Unit.Features.Accounts;

public class AccountServiceTests : IDisposable
{
    private readonly AppDbContext _dbContext;
    private readonly AccountService _service;

    public AccountServiceTests()
    {
        _dbContext = InMemoryDbContextFactory.Create();
        _service = new AccountService(_dbContext);
    }

    public void Dispose() => _dbContext.Dispose();

    // --- CreateAsync ---

    [Fact]
    public async Task CreateAsync_InstitutionExistsAndNameIsUnique_ReturnsSuccessWithAccount()
    {
        // Arrange
        var institution = SeedInstitution("River Bank");
        var request = new CreateAccountRequest("Checking", AccountType.Checking);

        // Act
        var result = await _service.CreateAsync(institution.Id, request);

        // Assert
        result.Success.Should().BeTrue();
        result.Data!.Name.Should().Be("Checking");
        result.Data.AccountType.Should().Be(AccountType.Checking);
    }

    [Fact]
    public async Task CreateAsync_InstitutionExistsAndNameIsUnique_PersistsAccountToDatabase()
    {
        // Arrange
        var institution = SeedInstitution("River Bank");
        var request = new CreateAccountRequest("Savings", AccountType.Savings);

        // Act
        await _service.CreateAsync(institution.Id, request);

        // Assert
        var saved = await _dbContext.Accounts.SingleAsync();
        saved.Name.Should().Be("Savings");
        saved.InstitutionId.Should().Be(institution.Id);
    }

    [Fact]
    public async Task CreateAsync_InstitutionDoesNotExist_ReturnsFailureResponse()
    {
        // Arrange
        var missingInstitutionId = Guid.NewGuid();
        var request = new CreateAccountRequest("Checking", AccountType.Checking);

        // Act
        var result = await _service.CreateAsync(missingInstitutionId, request);

        // Assert
        result.Success.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.Should().Contain(missingInstitutionId.ToString());
    }

    [Fact]
    public async Task CreateAsync_DuplicateNameInSameInstitution_ReturnsFailureResponse()
    {
        // Arrange
        var institution = SeedInstitution("River Bank");
        SeedAccount(institution.Id, "Checking", AccountType.Checking);
        var request = new CreateAccountRequest("Checking", AccountType.Savings);

        // Act
        var result = await _service.CreateAsync(institution.Id, request);

        // Assert
        result.Success.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.Should().Contain("Checking");
    }

    [Fact]
    public async Task CreateAsync_SameNameInDifferentInstitution_ReturnsSuccess()
    {
        // Arrange
        var institutionA = SeedInstitution("Bank A");
        var institutionB = SeedInstitution("Bank B");
        SeedAccount(institutionA.Id, "Checking", AccountType.Checking);
        var request = new CreateAccountRequest("Checking", AccountType.Checking);

        // Act
        var result = await _service.CreateAsync(institutionB.Id, request);

        // Assert
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task CreateAsync_NewAccount_ReturnsHasImportProfileFalse()
    {
        // Arrange
        var institution = SeedInstitution("Any Bank");
        var request = new CreateAccountRequest("Checking", AccountType.Checking);

        // Act
        var result = await _service.CreateAsync(institution.Id, request);

        // Assert
        result.Data!.HasImportProfile.Should().BeFalse();
    }

    // --- GetByIdAsync ---

    [Fact]
    public async Task GetByIdAsync_AccountExistsUnderInstitution_ReturnsSuccessWithAccount()
    {
        // Arrange
        var institution = SeedInstitution("River Bank");
        var account = SeedAccount(institution.Id, "Checking", AccountType.Checking);

        // Act
        var result = await _service.GetByIdAsync(institution.Id, account.Id);

        // Assert
        result.Success.Should().BeTrue();
        result.Data!.Id.Should().Be(account.Id);
        result.Data.Name.Should().Be("Checking");
    }

    [Fact]
    public async Task GetByIdAsync_AccountBelongsToDifferentInstitution_ReturnsFailureResponse()
    {
        // Arrange
        var institutionA = SeedInstitution("Bank A");
        var institutionB = SeedInstitution("Bank B");
        var account = SeedAccount(institutionA.Id, "Checking", AccountType.Checking);

        // Act
        var result = await _service.GetByIdAsync(institutionB.Id, account.Id);

        // Assert
        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task GetByIdAsync_AccountDoesNotExist_ReturnsFailureResponse()
    {
        // Arrange
        var institution = SeedInstitution("River Bank");
        var missingAccountId = Guid.NewGuid();

        // Act
        var result = await _service.GetByIdAsync(institution.Id, missingAccountId);

        // Assert
        result.Success.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.Should().Contain(missingAccountId.ToString());
    }

    [Fact]
    public async Task GetByIdAsync_AccountHasImportProfile_ReturnsHasImportProfileTrue()
    {
        // Arrange
        var institution = SeedInstitution("River Bank");
        var account = SeedAccount(institution.Id, "Credit Card", AccountType.CreditCard);
        SeedImportProfile(account.Id);

        // Act
        var result = await _service.GetByIdAsync(institution.Id, account.Id);

        // Assert
        result.Data!.HasImportProfile.Should().BeTrue();
    }

    [Fact]
    public async Task GetByIdAsync_AccountHasNoImportProfile_ReturnsHasImportProfileFalse()
    {
        // Arrange
        var institution = SeedInstitution("River Bank");
        var account = SeedAccount(institution.Id, "Checking", AccountType.Checking);

        // Act
        var result = await _service.GetByIdAsync(institution.Id, account.Id);

        // Assert
        result.Data!.HasImportProfile.Should().BeFalse();
    }

    // --- ListAsync ---

    [Fact]
    public async Task ListAsync_InstitutionDoesNotExist_ReturnsFailureResponse()
    {
        // Arrange
        var missingInstitutionId = Guid.NewGuid();

        // Act
        var result = await _service.ListAsync(missingInstitutionId);

        // Assert
        result.Success.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.Should().Contain(missingInstitutionId.ToString());
    }

    [Fact]
    public async Task ListAsync_InstitutionHasNoAccounts_ReturnsSuccessWithEmptyList()
    {
        // Arrange
        var institution = SeedInstitution("Empty Bank");

        // Act
        var result = await _service.ListAsync(institution.Id);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task ListAsync_InstitutionHasMultipleAccounts_ReturnsAllAccountsOrderedByName()
    {
        // Arrange
        var institution = SeedInstitution("River Bank");
        SeedAccount(institution.Id, "Savings", AccountType.Savings);
        SeedAccount(institution.Id, "Checking", AccountType.Checking);
        SeedAccount(institution.Id, "Money Market", AccountType.MoneyMarket);

        // Act
        var result = await _service.ListAsync(institution.Id);

        // Assert
        result.Data!.Select(a => a.Name)
            .Should().ContainInOrder("Checking", "Money Market", "Savings");
    }

    [Fact]
    public async Task ListAsync_MultipleInstitutions_ReturnsOnlyAccountsForRequestedInstitution()
    {
        // Arrange
        var institutionA = SeedInstitution("Bank A");
        var institutionB = SeedInstitution("Bank B");
        SeedAccount(institutionA.Id, "Checking A", AccountType.Checking);
        SeedAccount(institutionB.Id, "Checking B", AccountType.Checking);

        // Act
        var result = await _service.ListAsync(institutionA.Id);

        // Assert
        result.Data!.Should().HaveCount(1);
        result.Data!.Single().Name.Should().Be("Checking A");
    }

    // --- UpdateAsync ---

    [Fact]
    public async Task UpdateAsync_AccountExists_ReturnsSuccessWithUpdatedName()
    {
        // Arrange
        var institution = SeedInstitution("River Bank");
        var account = SeedAccount(institution.Id, "Old Name", AccountType.Checking);
        var request = new UpdateAccountRequest("New Name", AccountType.Checking);

        // Act
        var result = await _service.UpdateAsync(institution.Id, account.Id, request);

        // Assert
        result.Success.Should().BeTrue();
        result.Data!.Name.Should().Be("New Name");
    }

    [Fact]
    public async Task UpdateAsync_AccountExists_PersistsChangesToDatabase()
    {
        // Arrange
        var institution = SeedInstitution("River Bank");
        var account = SeedAccount(institution.Id, "Checking", AccountType.Checking);
        var request = new UpdateAccountRequest("Primary Checking", AccountType.CreditCard);

        // Act
        await _service.UpdateAsync(institution.Id, account.Id, request);

        // Assert
        var saved = await _dbContext.Accounts.FindAsync(account.Id);
        saved!.Name.Should().Be("Primary Checking");
        saved.AccountType.Should().Be(AccountType.CreditCard);
    }

    [Fact]
    public async Task UpdateAsync_AccountDoesNotExist_ReturnsFailureResponse()
    {
        // Arrange
        var institution = SeedInstitution("River Bank");
        var missingAccountId = Guid.NewGuid();
        var request = new UpdateAccountRequest("Any Name", AccountType.Checking);

        // Act
        var result = await _service.UpdateAsync(institution.Id, missingAccountId, request);

        // Assert
        result.Success.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.Should().Contain(missingAccountId.ToString());
    }

    [Fact]
    public async Task UpdateAsync_NewNameConflictsWithAnotherAccountInSameInstitution_ReturnsFailureResponse()
    {
        // Arrange
        var institution = SeedInstitution("River Bank");
        SeedAccount(institution.Id, "Beta", AccountType.Savings);
        var accountAlpha = SeedAccount(institution.Id, "Alpha", AccountType.Checking);
        var request = new UpdateAccountRequest("Beta", AccountType.Checking);

        // Act
        var result = await _service.UpdateAsync(institution.Id, accountAlpha.Id, request);

        // Assert
        result.Success.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.Should().Contain("Beta");
    }

    [Fact]
    public async Task UpdateAsync_NameUnchanged_ReturnsSuccess()
    {
        // Arrange
        var institution = SeedInstitution("River Bank");
        var account = SeedAccount(institution.Id, "Checking", AccountType.Checking);
        var request = new UpdateAccountRequest("Checking", AccountType.Savings);

        // Act
        var result = await _service.UpdateAsync(institution.Id, account.Id, request);

        // Assert
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateAsync_AccountBelongsToDifferentInstitution_ReturnsFailureResponse()
    {
        // Arrange
        var institutionA = SeedInstitution("Bank A");
        var institutionB = SeedInstitution("Bank B");
        var account = SeedAccount(institutionA.Id, "Checking", AccountType.Checking);
        var request = new UpdateAccountRequest("Updated", AccountType.Checking);

        // Act
        var result = await _service.UpdateAsync(institutionB.Id, account.Id, request);

        // Assert
        result.Success.Should().BeFalse();
    }

    // --- DeleteAsync ---

    [Fact]
    public async Task DeleteAsync_AccountExistsWithNoTransactions_ReturnsSuccessTrue()
    {
        // Arrange
        var institution = SeedInstitution("River Bank");
        var account = SeedAccount(institution.Id, "Checking", AccountType.Checking);

        // Act
        var result = await _service.DeleteAsync(institution.Id, account.Id);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteAsync_AccountExistsWithNoTransactions_RemovesAccountFromDatabase()
    {
        // Arrange
        var institution = SeedInstitution("River Bank");
        var account = SeedAccount(institution.Id, "To Be Deleted", AccountType.Checking);

        // Act
        await _service.DeleteAsync(institution.Id, account.Id);

        // Assert
        var remaining = await _dbContext.Accounts.CountAsync();
        remaining.Should().Be(0);
    }

    [Fact]
    public async Task DeleteAsync_AccountHasTransactions_ReturnsFailureResponse()
    {
        // Arrange
        var institution = SeedInstitution("River Bank");
        var account = SeedAccount(institution.Id, "Checking", AccountType.Checking);
        SeedTransaction(account.Id);

        // Act
        var result = await _service.DeleteAsync(institution.Id, account.Id);

        // Assert
        result.Success.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.Should().Contain("transactions");
    }

    [Fact]
    public async Task DeleteAsync_AccountHasTransactions_DoesNotRemoveAccountFromDatabase()
    {
        // Arrange
        var institution = SeedInstitution("River Bank");
        var account = SeedAccount(institution.Id, "Protected", AccountType.Checking);
        SeedTransaction(account.Id);

        // Act
        await _service.DeleteAsync(institution.Id, account.Id);

        // Assert
        var stillExists = await _dbContext.Accounts.AnyAsync(a => a.Id == account.Id);
        stillExists.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteAsync_AccountDoesNotExist_ReturnsFailureResponse()
    {
        // Arrange
        var institution = SeedInstitution("River Bank");
        var missingAccountId = Guid.NewGuid();

        // Act
        var result = await _service.DeleteAsync(institution.Id, missingAccountId);

        // Assert
        result.Success.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.Should().Contain(missingAccountId.ToString());
    }

    [Fact]
    public async Task DeleteAsync_AccountBelongsToDifferentInstitution_ReturnsFailureResponse()
    {
        // Arrange
        var institutionA = SeedInstitution("Bank A");
        var institutionB = SeedInstitution("Bank B");
        var account = SeedAccount(institutionA.Id, "Checking", AccountType.Checking);

        // Act
        var result = await _service.DeleteAsync(institutionB.Id, account.Id);

        // Assert
        result.Success.Should().BeFalse();
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

    private ImportProfile SeedImportProfile(Guid accountId)
    {
        var profile = new ImportProfile
        {
            AccountId = accountId,
            AmountType = AmountType.Single,
            BalanceProvided = false,
            DateFormat = "MM/dd/yyyy",
            Id = Guid.NewGuid(),
        };
        _dbContext.ImportProfiles.Add(profile);
        _dbContext.SaveChanges();
        return profile;
    }

    private Transaction SeedTransaction(Guid accountId)
    {
        var transaction = new Transaction
        {
            AccountId = accountId,
            Amount = 10.00m,
            Date = DateTime.UtcNow,
            Description = "Test Transaction",
            Id = Guid.NewGuid(),
            RawCsvRow = "2024-01-01,Test Transaction,10.00",
            RawData = new Dictionary<string, string>(),
        };
        _dbContext.Transactions.Add(transaction);
        _dbContext.SaveChanges();
        return transaction;
    }
}
