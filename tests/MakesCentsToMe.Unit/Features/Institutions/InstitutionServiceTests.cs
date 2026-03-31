// Feature: Institution Management
//
// Scenario: Create a new institution
//   Given a valid create request
//   When CreateAsync is called
//   Then the institution is persisted and returned in the response
//
// Scenario: Create institution persists with a new unique identifier
//   Given a valid create request
//   When CreateAsync is called
//   Then the returned institution has a non-empty Id
//
// Scenario: Get institution by id when institution exists
//   Given an institution exists in the database
//   When GetByIdAsync is called with that institution's id
//   Then the institution is returned with its account count
//
// Scenario: Get institution by id when institution does not exist
//   Given no institution exists with the requested id
//   When GetByIdAsync is called
//   Then a failure response is returned with a descriptive error
//
// Scenario: List institutions returns all institutions ordered by name
//   Given multiple institutions exist in the database
//   When ListAsync is called
//   Then all institutions are returned ordered alphabetically by name
//
// Scenario: List institutions returns empty list when no institutions exist
//   Given no institutions exist in the database
//   When ListAsync is called
//   Then an empty list is returned with a success response
//
// Scenario: Update institution name when institution exists
//   Given an institution exists in the database
//   When UpdateAsync is called with a new name
//   Then the institution's name is updated and the updated data is returned
//
// Scenario: Update institution when institution does not exist
//   Given no institution exists with the requested id
//   When UpdateAsync is called
//   Then a failure response is returned with a descriptive error
//
// Scenario: Delete institution when institution has no accounts
//   Given an institution exists with no accounts
//   When DeleteAsync is called
//   Then the institution is removed and a success response is returned
//
// Scenario: Delete institution when institution has accounts
//   Given an institution exists with at least one account
//   When DeleteAsync is called
//   Then a failure response is returned and the institution is not deleted
//
// Scenario: Delete institution when institution does not exist
//   Given no institution exists with the requested id
//   When DeleteAsync is called
//   Then a failure response is returned with a descriptive error
//
// Scenario: Account count is correctly reported in institution response
//   Given an institution with two accounts exists
//   When GetByIdAsync is called
//   Then the response includes AccountCount of 2

using FluentAssertions;
using MakesCentsToMe.Api.Features.Institutions;
using MakesCentsToMe.Api.Infrastructure.Data;
using MakesCentsToMe.Api.Models.Entities;
using MakesCentsToMe.Unit.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace MakesCentsToMe.Unit.Features.Institutions;

public class InstitutionServiceTests : IDisposable
{
    private readonly AppDbContext _dbContext;
    private readonly InstitutionService _service;

    public InstitutionServiceTests()
    {
        _dbContext = InMemoryDbContextFactory.Create();
        _service = new InstitutionService(_dbContext);
    }

    public void Dispose() => _dbContext.Dispose();

    // --- CreateAsync ---

    [Fact]
    public async Task CreateAsync_ValidRequest_ReturnsSuccessWithInstitution()
    {
        // Arrange
        var request = new CreateInstitutionRequest("First National Bank");

        // Act
        var result = await _service.CreateAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Name.Should().Be("First National Bank");
    }

    [Fact]
    public async Task CreateAsync_ValidRequest_PersistsInstitutionToDatabase()
    {
        // Arrange
        var request = new CreateInstitutionRequest("Savings Credit Union");

        // Act
        await _service.CreateAsync(request);

        // Assert
        var saved = await _dbContext.Institutions.SingleAsync();
        saved.Name.Should().Be("Savings Credit Union");
    }

    [Fact]
    public async Task CreateAsync_ValidRequest_AssignsNonEmptyId()
    {
        // Arrange
        var request = new CreateInstitutionRequest("Any Bank");

        // Act
        var result = await _service.CreateAsync(request);

        // Assert
        result.Data!.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task CreateAsync_ValidRequest_ReturnsZeroAccountCount()
    {
        // Arrange
        var request = new CreateInstitutionRequest("New Bank");

        // Act
        var result = await _service.CreateAsync(request);

        // Assert
        result.Data!.AccountCount.Should().Be(0);
    }

    // --- GetByIdAsync ---

    [Fact]
    public async Task GetByIdAsync_InstitutionExists_ReturnsSuccessWithInstitution()
    {
        // Arrange
        var institution = SeedInstitution("River Bank");

        // Act
        var result = await _service.GetByIdAsync(institution.Id);

        // Assert
        result.Success.Should().BeTrue();
        result.Data!.Id.Should().Be(institution.Id);
        result.Data.Name.Should().Be("River Bank");
    }

    [Fact]
    public async Task GetByIdAsync_InstitutionExistsWithTwoAccounts_ReturnsAccountCountOfTwo()
    {
        // Arrange
        var institution = SeedInstitution("Metro Credit Union");
        SeedAccount(institution.Id, "Checking", AccountType.Checking);
        SeedAccount(institution.Id, "Savings", AccountType.Savings);

        // Act
        var result = await _service.GetByIdAsync(institution.Id);

        // Assert
        result.Data!.AccountCount.Should().Be(2);
    }

    [Fact]
    public async Task GetByIdAsync_InstitutionDoesNotExist_ReturnsFailureResponse()
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
    public async Task ListAsync_NoInstitutionsExist_ReturnsSuccessWithEmptyList()
    {
        // Act
        var result = await _service.ListAsync();

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task ListAsync_MultipleInstitutionsExist_ReturnsAllInstitutionsOrderedByName()
    {
        // Arrange
        SeedInstitution("Zebra Bank");
        SeedInstitution("Alpha Credit Union");
        SeedInstitution("Metro Savings");

        // Act
        var result = await _service.ListAsync();

        // Assert
        result.Success.Should().BeTrue();
        result.Data!.Select(i => i.Name)
            .Should().ContainInOrder("Alpha Credit Union", "Metro Savings", "Zebra Bank");
    }

    [Fact]
    public async Task ListAsync_InstitutionWithAccounts_ReturnsCorrectAccountCount()
    {
        // Arrange
        var institution = SeedInstitution("River Bank");
        SeedAccount(institution.Id, "Checking", AccountType.Checking);

        // Act
        var result = await _service.ListAsync();

        // Assert
        result.Data!.Single().AccountCount.Should().Be(1);
    }

    // --- UpdateAsync ---

    [Fact]
    public async Task UpdateAsync_InstitutionExists_ReturnsSuccessWithUpdatedName()
    {
        // Arrange
        var institution = SeedInstitution("Old Name Bank");
        var request = new UpdateInstitutionRequest("New Name Bank");

        // Act
        var result = await _service.UpdateAsync(institution.Id, request);

        // Assert
        result.Success.Should().BeTrue();
        result.Data!.Name.Should().Be("New Name Bank");
    }

    [Fact]
    public async Task UpdateAsync_InstitutionExists_PersistsNameChangeToDatabase()
    {
        // Arrange
        var institution = SeedInstitution("Before Update");
        var request = new UpdateInstitutionRequest("After Update");

        // Act
        await _service.UpdateAsync(institution.Id, request);

        // Assert
        var saved = await _dbContext.Institutions.FindAsync(institution.Id);
        saved!.Name.Should().Be("After Update");
    }

    [Fact]
    public async Task UpdateAsync_InstitutionDoesNotExist_ReturnsFailureResponse()
    {
        // Arrange
        var missingId = Guid.NewGuid();
        var request = new UpdateInstitutionRequest("Any Name");

        // Act
        var result = await _service.UpdateAsync(missingId, request);

        // Assert
        result.Success.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.Should().Contain(missingId.ToString());
    }

    // --- DeleteAsync ---

    [Fact]
    public async Task DeleteAsync_InstitutionExistsWithNoAccounts_ReturnsSuccessTrue()
    {
        // Arrange
        var institution = SeedInstitution("Empty Institution");

        // Act
        var result = await _service.DeleteAsync(institution.Id);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteAsync_InstitutionExistsWithNoAccounts_RemovesInstitutionFromDatabase()
    {
        // Arrange
        var institution = SeedInstitution("To Be Deleted");

        // Act
        await _service.DeleteAsync(institution.Id);

        // Assert
        var remaining = await _dbContext.Institutions.CountAsync();
        remaining.Should().Be(0);
    }

    [Fact]
    public async Task DeleteAsync_InstitutionHasAccounts_ReturnsFailureResponse()
    {
        // Arrange
        var institution = SeedInstitution("Bank With Accounts");
        SeedAccount(institution.Id, "Checking", AccountType.Checking);

        // Act
        var result = await _service.DeleteAsync(institution.Id);

        // Assert
        result.Success.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.Should().Contain("accounts");
    }

    [Fact]
    public async Task DeleteAsync_InstitutionHasAccounts_DoesNotRemoveInstitutionFromDatabase()
    {
        // Arrange
        var institution = SeedInstitution("Protected Bank");
        SeedAccount(institution.Id, "Savings", AccountType.Savings);

        // Act
        await _service.DeleteAsync(institution.Id);

        // Assert
        var stillExists = await _dbContext.Institutions.AnyAsync(i => i.Id == institution.Id);
        stillExists.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteAsync_InstitutionDoesNotExist_ReturnsFailureResponse()
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
}
