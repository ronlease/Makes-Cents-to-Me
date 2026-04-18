// Feature: Category Management
//
// Scenario: Create a new category with a unique name
//   Given no category with the same name exists
//   When CreateAsync is called with a valid name
//   Then the category is persisted and returned in the response
//
// Scenario: Create category assigns a non-empty identifier
//   Given a valid create request
//   When CreateAsync is called
//   Then the returned category has a non-empty Id
//
// Scenario: Create category sets IsDefault to false
//   Given a valid create request
//   When CreateAsync is called
//   Then the returned category has IsDefault equal to false
//
// Scenario: Create category returns zero transaction count
//   Given a valid create request
//   When CreateAsync is called
//   Then the returned category has TransactionCount of zero
//
// Scenario: Create category with duplicate name fails
//   Given a category named "Groceries" already exists
//   When CreateAsync is called with the name "Groceries"
//   Then a failure response is returned citing the duplicate name
//
// Scenario: Delete category when category exists with no transactions
//   Given a category exists with no transactions assigned
//   When DeleteAsync is called
//   Then the category is removed and a success response is returned
//
// Scenario: Delete category removes it from the database
//   Given a category exists with no transactions assigned
//   When DeleteAsync is called
//   Then the category no longer exists in the database
//
// Scenario: Delete category when category has assigned transactions
//   Given a category exists with at least one transaction assigned
//   When DeleteAsync is called
//   Then a failure response is returned citing assigned transactions
//
// Scenario: Delete category with transactions does not remove the category
//   Given a category exists with one transaction assigned
//   When DeleteAsync is called
//   Then the category still exists in the database
//
// Scenario: Delete category when category does not exist
//   Given no category exists with the requested id
//   When DeleteAsync is called
//   Then a failure response is returned citing the missing id
//
// Scenario: Get category by id when category exists
//   Given a category exists in the database
//   When GetByIdAsync is called with that category's id
//   Then the category is returned with its correct transaction count
//
// Scenario: Get category by id when category does not exist
//   Given no category exists with the requested id
//   When GetByIdAsync is called
//   Then a failure response is returned citing the missing id
//
// Scenario: List returns all categories ordered by name
//   Given multiple categories exist in the database
//   When ListAsync is called
//   Then all categories are returned ordered alphabetically by name
//
// Scenario: List returns empty list when no categories exist
//   Given no categories exist in the database
//   When ListAsync is called
//   Then an empty list is returned with a success response
//
// Scenario: List returns correct transaction count per category
//   Given a category exists with two transactions assigned
//   When ListAsync is called
//   Then the category's TransactionCount is two
//
// Scenario: Update category name when category exists
//   Given a category exists in the database
//   When UpdateAsync is called with a new name
//   Then the category name is updated and the updated data is returned
//
// Scenario: Update category persists the new name to the database
//   Given a category exists in the database
//   When UpdateAsync is called with a new name
//   Then the persisted entity reflects the updated name
//
// Scenario: Update category when category does not exist
//   Given no category exists with the requested id
//   When UpdateAsync is called
//   Then a failure response is returned citing the missing id
//
// Scenario: Update category to a name already used by another category
//   Given two categories exist with names "Groceries" and "Dining"
//   When UpdateAsync is called on "Groceries" using the name "Dining"
//   Then a failure response is returned citing the duplicate name
//
// Scenario: Update category to its own current name succeeds
//   Given a category named "Groceries" exists
//   When UpdateAsync is called on that same category with the name "Groceries"
//   Then a success response is returned

using FluentAssertions;
using MakesCentsToMe.Api.Features.Categories;
using MakesCentsToMe.Api.Infrastructure.Data;
using MakesCentsToMe.Api.Models.Entities;
using MakesCentsToMe.Unit.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace MakesCentsToMe.Unit.Features.Categories;

public class CategoryServiceTests : IDisposable
{
    private readonly AppDbContext _dbContext;
    private readonly CategoryService _service;

    public CategoryServiceTests()
    {
        _dbContext = InMemoryDbContextFactory.Create();
        _service = new CategoryService(_dbContext);
    }

    public void Dispose() => _dbContext.Dispose();

    // --- CreateAsync ---

    [Fact]
    public async Task CreateAsync_UniqueName_ReturnsSuccessWithCategory()
    {
        // Arrange
        var request = new CreateCategoryRequest("Groceries");

        // Act
        var result = await _service.CreateAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Name.Should().Be("Groceries");
    }

    [Fact]
    public async Task CreateAsync_UniqueName_AssignsNonEmptyId()
    {
        // Arrange
        var request = new CreateCategoryRequest("Groceries");

        // Act
        var result = await _service.CreateAsync(request);

        // Assert
        result.Data!.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task CreateAsync_UniqueName_SetsIsDefaultToFalse()
    {
        // Arrange
        var request = new CreateCategoryRequest("Dining");

        // Act
        var result = await _service.CreateAsync(request);

        // Assert
        result.Data!.IsDefault.Should().BeFalse();
    }

    [Fact]
    public async Task CreateAsync_UniqueName_ReturnsZeroTransactionCount()
    {
        // Arrange
        var request = new CreateCategoryRequest("Transportation");

        // Act
        var result = await _service.CreateAsync(request);

        // Assert
        result.Data!.TransactionCount.Should().Be(0);
    }

    [Fact]
    public async Task CreateAsync_UniqueName_PersistsCategoryToDatabase()
    {
        // Arrange
        var request = new CreateCategoryRequest("Utilities");

        // Act
        await _service.CreateAsync(request);

        // Assert
        var saved = await _dbContext.Categories.SingleAsync();
        saved.Name.Should().Be("Utilities");
    }

    [Fact]
    public async Task CreateAsync_DuplicateName_ReturnsFailureResponseCitingDuplicateName()
    {
        // Arrange
        SeedCategory("Groceries");
        var request = new CreateCategoryRequest("Groceries");

        // Act
        var result = await _service.CreateAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.Should().Contain("Groceries");
    }

    [Fact]
    public async Task CreateAsync_DuplicateName_DoesNotCreateAdditionalCategory()
    {
        // Arrange
        SeedCategory("Groceries");
        var request = new CreateCategoryRequest("Groceries");

        // Act
        await _service.CreateAsync(request);

        // Assert
        var count = await _dbContext.Categories.CountAsync();
        count.Should().Be(1);
    }

    // --- DeleteAsync ---

    [Fact]
    public async Task DeleteAsync_CategoryExistsWithNoTransactions_ReturnsSuccessTrue()
    {
        // Arrange
        var category = SeedCategory("Entertainment");

        // Act
        var result = await _service.DeleteAsync(category.Id);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteAsync_CategoryExistsWithNoTransactions_RemovesCategoryFromDatabase()
    {
        // Arrange
        var category = SeedCategory("Entertainment");

        // Act
        await _service.DeleteAsync(category.Id);

        // Assert
        var remaining = await _dbContext.Categories.CountAsync();
        remaining.Should().Be(0);
    }

    [Fact]
    public async Task DeleteAsync_CategoryHasAssignedTransactions_ReturnsFailureResponse()
    {
        // Arrange
        var category = SeedCategory("Dining");
        SeedTransactionForCategory(category.Id);

        // Act
        var result = await _service.DeleteAsync(category.Id);

        // Assert
        result.Success.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.Should().Contain("transactions");
    }

    [Fact]
    public async Task DeleteAsync_CategoryHasAssignedTransactions_DoesNotRemoveCategoryFromDatabase()
    {
        // Arrange
        var category = SeedCategory("Dining");
        SeedTransactionForCategory(category.Id);

        // Act
        await _service.DeleteAsync(category.Id);

        // Assert
        var stillExists = await _dbContext.Categories.AnyAsync(c => c.Id == category.Id);
        stillExists.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteAsync_CategoryDoesNotExist_ReturnsFailureResponseCitingMissingId()
    {
        // Arrange
        var missingId = Guid.NewGuid();

        // Act
        var result = await _service.DeleteAsync(missingId);

        // Assert
        result.Success.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.Should().Contain(missingId.ToString());
    }

    // --- GetByIdAsync ---

    [Fact]
    public async Task GetByIdAsync_CategoryExists_ReturnsSuccessWithCategory()
    {
        // Arrange
        var category = SeedCategory("Healthcare");

        // Act
        var result = await _service.GetByIdAsync(category.Id);

        // Assert
        result.Success.Should().BeTrue();
        result.Data!.Id.Should().Be(category.Id);
        result.Data.Name.Should().Be("Healthcare");
    }

    [Fact]
    public async Task GetByIdAsync_CategoryExistsWithTwoTransactions_ReturnsTransactionCountOfTwo()
    {
        // Arrange
        var category = SeedCategory("Groceries");
        SeedTransactionForCategory(category.Id);
        SeedTransactionForCategory(category.Id);

        // Act
        var result = await _service.GetByIdAsync(category.Id);

        // Assert
        result.Data!.TransactionCount.Should().Be(2);
    }

    [Fact]
    public async Task GetByIdAsync_CategoryDoesNotExist_ReturnsFailureResponseCitingMissingId()
    {
        // Arrange
        var missingId = Guid.NewGuid();

        // Act
        var result = await _service.GetByIdAsync(missingId);

        // Assert
        result.Success.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.Should().Contain(missingId.ToString());
    }

    // --- ListAsync ---

    [Fact]
    public async Task ListAsync_NoCategoriesExist_ReturnsSuccessWithEmptyList()
    {
        // Act
        var result = await _service.ListAsync();

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task ListAsync_MultipleCategoriesExist_ReturnsAllCategoriesOrderedByName()
    {
        // Arrange
        SeedCategory("Utilities");
        SeedCategory("Dining");
        SeedCategory("Groceries");

        // Act
        var result = await _service.ListAsync();

        // Assert
        result.Success.Should().BeTrue();
        result.Data!.Select(c => c.Name)
            .Should().ContainInOrder("Dining", "Groceries", "Utilities");
    }

    [Fact]
    public async Task ListAsync_CategoryWithTwoTransactions_ReturnsCorrectTransactionCount()
    {
        // Arrange
        var category = SeedCategory("Groceries");
        SeedTransactionForCategory(category.Id);
        SeedTransactionForCategory(category.Id);

        // Act
        var result = await _service.ListAsync();

        // Assert
        result.Data!.Single().TransactionCount.Should().Be(2);
    }

    // --- UpdateAsync ---

    [Fact]
    public async Task UpdateAsync_CategoryExists_ReturnsSuccessWithUpdatedName()
    {
        // Arrange
        var category = SeedCategory("Old Name");
        var request = new UpdateCategoryRequest("New Name");

        // Act
        var result = await _service.UpdateAsync(category.Id, request);

        // Assert
        result.Success.Should().BeTrue();
        result.Data!.Name.Should().Be("New Name");
    }

    [Fact]
    public async Task UpdateAsync_CategoryExists_PersistsNameChangeToDatabase()
    {
        // Arrange
        var category = SeedCategory("Before Update");
        var request = new UpdateCategoryRequest("After Update");

        // Act
        await _service.UpdateAsync(category.Id, request);

        // Assert
        var saved = await _dbContext.Categories.FindAsync(category.Id);
        saved!.Name.Should().Be("After Update");
    }

    [Fact]
    public async Task UpdateAsync_CategoryDoesNotExist_ReturnsFailureResponseCitingMissingId()
    {
        // Arrange
        var missingId = Guid.NewGuid();
        var request = new UpdateCategoryRequest("Any Name");

        // Act
        var result = await _service.UpdateAsync(missingId, request);

        // Assert
        result.Success.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.Should().Contain(missingId.ToString());
    }

    [Fact]
    public async Task UpdateAsync_NameAlreadyUsedByAnotherCategory_ReturnsFailureResponseCitingDuplicateName()
    {
        // Arrange
        var category = SeedCategory("Groceries");
        SeedCategory("Dining");
        var request = new UpdateCategoryRequest("Dining");

        // Act
        var result = await _service.UpdateAsync(category.Id, request);

        // Assert
        result.Success.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.Should().Contain("Dining");
    }

    [Fact]
    public async Task UpdateAsync_SameNameAsCurrentCategory_ReturnsSuccess()
    {
        // Arrange
        var category = SeedCategory("Groceries");
        var request = new UpdateCategoryRequest("Groceries");

        // Act
        var result = await _service.UpdateAsync(category.Id, request);

        // Assert
        result.Success.Should().BeTrue();
        result.Data!.Name.Should().Be("Groceries");
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

    private Transaction SeedTransactionForCategory(Guid categoryId)
    {
        var institution = new Institution { Id = Guid.NewGuid(), Name = $"Bank {Guid.NewGuid()}" };
        _dbContext.Institutions.Add(institution);

        var account = new Account
        {
            AccountType = AccountType.Checking,
            Id = Guid.NewGuid(),
            InstitutionId = institution.Id,
            Name = "Checking",
        };
        _dbContext.Accounts.Add(account);

        var transaction = new Transaction
        {
            AccountId = account.Id,
            Amount = 10.00m,
            CategoryId = categoryId,
            Date = DateTime.UtcNow,
            Description = "Test Transaction",
            Id = Guid.NewGuid(),
            RawCsvRow = "2024-01-01,Test Transaction,10.00",
            Status = TransactionStatus.Committed,
        };
        _dbContext.Transactions.Add(transaction);
        _dbContext.SaveChanges();
        return transaction;
    }
}
