// Feature: Learned Rules
//
// Scenario: Create persists a rule with a normalized pattern
//   Given a valid category exists
//   When CreateAsync is called with a lower case pattern
//   Then the rule is persisted with an upper case pattern
//
// Scenario: Create with a duplicate pattern fails
//   Given a rule with pattern "WHOLEFDS MKT" exists
//   When CreateAsync is called with the same pattern in a different case
//   Then a failure response is returned
//
// Scenario: Create with an unknown category fails
//   Given no category exists with the requested id
//   When CreateAsync is called
//   Then a failure response is returned
//
// Scenario: Create with a blank pattern fails
//   When CreateAsync is called with a whitespace pattern
//   Then a failure response is returned
//
// Scenario: Create with a pattern shorter than three characters fails
//   When CreateAsync is called with the pattern "AB"
//   Then a failure response is returned
//
// Scenario: List returns pattern, vendor and category for every rule
//   Given ten rules exist
//   When ListAsync is called
//   Then ten rules are returned with their pattern, vendor and category name
//
// Scenario: List returns an empty list when no rules exist
//   When ListAsync is called
//   Then a successful response with an empty list is returned
//
// Scenario: Get by id for a missing rule is not found
//   When GetByIdAsync is called with an unknown id
//   Then the result is flagged as not found
//
// Scenario: Update changes the category and refreshes UpdatedAt
//   Given a rule exists
//   When UpdateAsync is called with a different category
//   Then the new category is persisted and UpdatedAt is later than before
//
// Scenario: Update for a missing rule is not found
//   When UpdateAsync is called with an unknown id
//   Then the result is flagged as not found
//
// Scenario: Update colliding with another rule fails
//   Given two rules exist
//   When the first is updated to use the second rule's pattern
//   Then a failure response is returned
//
// Scenario: Delete removes the rule
//   Given a rule exists
//   When DeleteAsync is called
//   Then the rule no longer exists
//
// Scenario: Delete for a missing rule is not found
//   When DeleteAsync is called with an unknown id
//   Then the result is flagged as not found
//
// Scenario: Delete clears the rule reference but keeps the auto categorized flag
//   Given a transaction was auto categorized by a rule
//   When the rule is deleted
//   Then the transaction LearnedRuleId is null and IsAutoCategorized remains true
//
// Scenario: Applying rules commits a matching transaction as auto categorized
//   Given a rule matches a pending transaction description
//   When ApplyRulesAsync is called
//   Then the transaction is committed with confidence 1.0, vendor and category from the rule
//   And the description and raw data are unchanged
//   And the transaction is not returned as unmatched
//
// Scenario: Applying rules returns unmatched transactions
//   Given no rule matches a transaction
//   When ApplyRulesAsync is called
//   Then the transaction is returned and left pending
//
// Scenario: Longest matching pattern wins when applying rules
//   Given rules "AMZN" and "AMZN MKTP" both match a transaction
//   When ApplyRulesAsync is called
//   Then the "AMZN MKTP" rule is applied
//
// Scenario: Applying rules respects word boundaries
//   Given a rule "WHOLEFDS MKT" and a transaction "WHOLEFDS MKTPLACE 1"
//   When ApplyRulesAsync is called
//   Then the transaction is returned as unmatched
//
// Scenario: Applying rules after an update uses the new category
//   Given a rule was updated to a different category
//   When ApplyRulesAsync is called
//   Then the transaction receives the new category
//
// Scenario: Applying rules after a delete returns the transaction as unmatched
//   Given a rule was deleted
//   When ApplyRulesAsync is called
//   Then the transaction is returned as unmatched
//
// Scenario: Rule for AMZN MKTP matches a generated reference suffix
//   Given a rule "AMZN MKTP" exists
//   When ApplyRulesAsync is called for "AMZN MKTP US*9X8Y7Z6W5"
//   Then the transaction is auto categorized by that rule
//
// Scenario: Suggestion is built when the user overrides Claude
//   Given Claude suggested Shopping and the user chose Groceries with vendor "Whole Foods"
//   When BuildSuggestionAsync is called for "WHOLEFDS MKT 10245"
//   Then a suggestion with pattern "WHOLEFDS MKT" is returned
//
// Scenario: No suggestion without a category
//   Given a transaction with no category
//   When BuildSuggestionAsync is called
//   Then null is returned
//
// Scenario: No suggestion when the user accepts the Claude suggestion
//   Given the committed category and vendor equal the Claude suggestion
//   When BuildSuggestionAsync is called
//   Then null is returned
//
// Scenario: No suggestion when an identical rule already exists
//   Given a rule with the same pattern, vendor and category exists
//   When BuildSuggestionAsync is called
//   Then null is returned
//
// Scenario: Suggestion references an existing rule with a different mapping
//   Given a rule with the same pattern but a different category exists
//   When BuildSuggestionAsync is called
//   Then a suggestion carrying the existing rule id is returned

using FluentAssertions;
using MakesCentsToMe.Api.Features.LearnedRules;
using MakesCentsToMe.Api.Infrastructure.Data;
using MakesCentsToMe.Api.Models.Entities;
using MakesCentsToMe.Unit.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace MakesCentsToMe.Unit.Features.LearnedRules;

public class LearnedRuleServiceTests : IDisposable
{
    private readonly Account _account;
    private readonly AppDbContext _dbContext;
    private readonly LearnedRuleService _service;

    public LearnedRuleServiceTests()
    {
        _dbContext = InMemoryDbContextFactory.Create();
        _service = new LearnedRuleService(_dbContext);

        var institution = new Institution { Id = Guid.NewGuid(), Name = "Test Bank" };
        _account = new Account
        {
            AccountType = AccountType.Checking,
            Id = Guid.NewGuid(),
            InstitutionId = institution.Id,
            Name = "Checking",
        };
        _dbContext.Institutions.Add(institution);
        _dbContext.Accounts.Add(_account);
        _dbContext.SaveChanges();
    }

    public void Dispose() => _dbContext.Dispose();

    [Fact]
    public async Task ApplyRulesAsync_AfterRuleDeleted_ReturnsTransactionAsUnmatched()
    {
        // Arrange
        var groceries = SeedCategory("Groceries");
        var rule = SeedRule("WHOLEFDS MKT", "Whole Foods", groceries);
        await _service.DeleteAsync(rule.Id);
        var transaction = CreateTransaction("WHOLEFDS MKT 10245");

        // Act
        var unmatched = await _service.ApplyRulesAsync([transaction]);

        // Assert
        unmatched.Should().ContainSingle().Which.Should().BeSameAs(transaction);
        transaction.Status.Should().Be(TransactionStatus.Pending);
    }

    [Fact]
    public async Task ApplyRulesAsync_AfterRuleUpdated_UsesNewCategory()
    {
        // Arrange
        var groceries = SeedCategory("Groceries");
        var household = SeedCategory("Household");
        var rule = SeedRule("WHOLEFDS MKT", "Whole Foods", groceries);
        await _service.UpdateAsync(rule.Id, new UpdateLearnedRuleRequest(household.Id, "Whole Foods", "WHOLEFDS MKT"));
        var transaction = CreateTransaction("WHOLEFDS MKT 10245");

        // Act
        await _service.ApplyRulesAsync([transaction]);

        // Assert
        transaction.CategoryId.Should().Be(household.Id);
    }

    [Fact]
    public async Task ApplyRulesAsync_AmznMarketplaceRuleWithReferenceSuffix_AutoCategorizes()
    {
        // Arrange
        var shopping = SeedCategory("Shopping");
        var rule = SeedRule("AMZN MKTP", "Amazon", shopping);
        var transaction = CreateTransaction("AMZN MKTP US*9X8Y7Z6W5");

        // Act
        var unmatched = await _service.ApplyRulesAsync([transaction]);

        // Assert
        unmatched.Should().BeEmpty();
        transaction.LearnedRuleId.Should().Be(rule.Id);
        transaction.CategoryId.Should().Be(shopping.Id);
        transaction.IsAutoCategorized.Should().BeTrue();
    }

    [Fact]
    public async Task ApplyRulesAsync_MatchingRule_CommitsAsAutoCategorizedWithFullConfidence()
    {
        // Arrange
        var groceries = SeedCategory("Groceries");
        var rule = SeedRule("WHOLEFDS MKT", "Whole Foods", groceries);
        var transaction = CreateTransaction("WHOLEFDS MKT 10245");

        // Act
        var unmatched = await _service.ApplyRulesAsync([transaction]);

        // Assert
        unmatched.Should().BeEmpty();
        transaction.Status.Should().Be(TransactionStatus.Committed);
        transaction.IsAutoCategorized.Should().BeTrue();
        transaction.Confidence.Should().Be(1.0m);
        transaction.CategoryId.Should().Be(groceries.Id);
        transaction.NormalizedVendor.Should().Be("Whole Foods");
        transaction.LearnedRuleId.Should().Be(rule.Id);
    }

    [Fact]
    public async Task ApplyRulesAsync_MatchingRule_DoesNotModifyDescriptionOrRawData()
    {
        // Arrange
        var groceries = SeedCategory("Groceries");
        SeedRule("WHOLEFDS MKT", "Whole Foods", groceries);
        var transaction = CreateTransaction("WHOLEFDS MKT 10245");
        transaction.RawData = new Dictionary<string, string> { ["Description"] = "WHOLEFDS MKT 10245" };

        // Act
        await _service.ApplyRulesAsync([transaction]);

        // Assert
        transaction.Description.Should().Be("WHOLEFDS MKT 10245");
        transaction.RawCsvRow.Should().Be("raw,WHOLEFDS MKT 10245");
        transaction.RawData.Should().ContainSingle()
            .Which.Should().Be(new KeyValuePair<string, string>("Description", "WHOLEFDS MKT 10245"));
    }

    [Fact]
    public async Task ApplyRulesAsync_MultipleMatchingRules_AppliesLongestPattern()
    {
        // Arrange
        var shopping = SeedCategory("Shopping");
        var household = SeedCategory("Household");
        SeedRule("AMZN", "Amazon Generic", household);
        var longer = SeedRule("AMZN MKTP", "Amazon Marketplace", shopping);
        var transaction = CreateTransaction("AMZN MKTP US*9X8Y7Z6W5");

        // Act
        await _service.ApplyRulesAsync([transaction]);

        // Assert
        transaction.LearnedRuleId.Should().Be(longer.Id);
        transaction.CategoryId.Should().Be(shopping.Id);
    }

    [Fact]
    public async Task ApplyRulesAsync_NoRuleMatches_ReturnsTransactionAsPending()
    {
        // Arrange
        var groceries = SeedCategory("Groceries");
        SeedRule("WHOLEFDS MKT", "Whole Foods", groceries);
        var transaction = CreateTransaction("TARGET 00012");

        // Act
        var unmatched = await _service.ApplyRulesAsync([transaction]);

        // Assert
        unmatched.Should().ContainSingle().Which.Should().BeSameAs(transaction);
        transaction.Status.Should().Be(TransactionStatus.Pending);
        transaction.LearnedRuleId.Should().BeNull();
    }

    [Fact]
    public async Task ApplyRulesAsync_PatternContinuesIntoLongerWord_ReturnsTransactionAsUnmatched()
    {
        // Arrange
        var groceries = SeedCategory("Groceries");
        SeedRule("WHOLEFDS MKT", "Whole Foods", groceries);
        var transaction = CreateTransaction("WHOLEFDS MKTPLACE 1");

        // Act
        var unmatched = await _service.ApplyRulesAsync([transaction]);

        // Assert
        unmatched.Should().ContainSingle();
    }

    [Fact]
    public async Task BuildSuggestionAsync_ExistingRuleWithDifferentMapping_ReturnsSuggestionWithExistingRuleId()
    {
        // Arrange
        var shopping = SeedCategory("Shopping");
        var groceries = SeedCategory("Groceries");
        var existing = SeedRule("WHOLEFDS MKT", "Whole Foods", shopping);
        var transaction = CreateTransaction("WHOLEFDS MKT 10245");
        transaction.CategoryId = groceries.Id;
        transaction.NormalizedVendor = "Whole Foods";

        // Act
        var suggestion = await _service.BuildSuggestionAsync(transaction);

        // Assert
        suggestion.Should().NotBeNull();
        suggestion!.ExistingLearnedRuleId.Should().Be(existing.Id);
        suggestion.CategoryId.Should().Be(groceries.Id);
    }

    [Fact]
    public async Task BuildSuggestionAsync_IdenticalRuleExists_ReturnsNull()
    {
        // Arrange
        var shopping = SeedCategory("Shopping");
        var groceries = SeedCategory("Groceries");
        SeedRule("WHOLEFDS MKT", "Whole Foods", groceries);
        var transaction = CreateTransaction("WHOLEFDS MKT 10245");
        transaction.CategoryId = groceries.Id;
        transaction.NormalizedVendor = "Whole Foods";
        transaction.SuggestedCategoryId = shopping.Id;
        transaction.SuggestedNormalizedVendor = "Whole Foods";

        // Act
        var suggestion = await _service.BuildSuggestionAsync(transaction);

        // Assert
        suggestion.Should().BeNull();
    }

    [Fact]
    public async Task BuildSuggestionAsync_NullCategory_ReturnsNull()
    {
        // Arrange
        var transaction = CreateTransaction("WHOLEFDS MKT 10245");
        transaction.CategoryId = null;
        transaction.NormalizedVendor = "Whole Foods";

        // Act
        var suggestion = await _service.BuildSuggestionAsync(transaction);

        // Assert
        suggestion.Should().BeNull();
    }

    [Fact]
    public async Task BuildSuggestionAsync_OverrideOfClaudeSuggestion_SuggestsDerivedPattern()
    {
        // Arrange
        var shopping = SeedCategory("Shopping");
        var groceries = SeedCategory("Groceries");
        var transaction = CreateTransaction("WHOLEFDS MKT 10245");
        transaction.CategoryId = groceries.Id;
        transaction.NormalizedVendor = "Whole Foods";
        transaction.SuggestedCategoryId = shopping.Id;
        transaction.SuggestedNormalizedVendor = "Wholefds Market";

        // Act
        var suggestion = await _service.BuildSuggestionAsync(transaction);

        // Assert
        suggestion.Should().NotBeNull();
        suggestion!.Pattern.Should().Be("WHOLEFDS MKT");
        suggestion.NormalizedVendor.Should().Be("Whole Foods");
        suggestion.CategoryId.Should().Be(groceries.Id);
        suggestion.CategoryName.Should().Be("Groceries");
        suggestion.ExistingLearnedRuleId.Should().BeNull();
        suggestion.SourceTransactionId.Should().Be(transaction.Id);
    }

    [Fact]
    public async Task BuildSuggestionAsync_SameAsClaudeSuggestion_ReturnsNull()
    {
        // Arrange
        var groceries = SeedCategory("Groceries");
        var transaction = CreateTransaction("WHOLEFDS MKT 10245");
        transaction.CategoryId = groceries.Id;
        transaction.NormalizedVendor = "Whole Foods";
        transaction.SuggestedCategoryId = groceries.Id;
        transaction.SuggestedNormalizedVendor = "whole foods";

        // Act
        var suggestion = await _service.BuildSuggestionAsync(transaction);

        // Assert
        suggestion.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_BlankPattern_ReturnsFailure()
    {
        // Arrange
        var groceries = SeedCategory("Groceries");

        // Act
        var result = await _service.CreateAsync(new CreateLearnedRuleRequest(groceries.Id, "Whole Foods", "   ", null));

        // Assert
        result.Success.Should().BeFalse();
        result.IsNotFound.Should().BeFalse();
        (await _dbContext.LearnedRules.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task CreateAsync_DuplicatePattern_ReturnsFailure()
    {
        // Arrange
        var groceries = SeedCategory("Groceries");
        SeedRule("WHOLEFDS MKT", "Whole Foods", groceries);

        // Act
        var result = await _service.CreateAsync(new CreateLearnedRuleRequest(groceries.Id, "Whole Foods", "wholefds mkt", null));

        // Assert
        result.Success.Should().BeFalse();
        (await _dbContext.LearnedRules.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task CreateAsync_PatternShorterThanMinimum_ReturnsFailure()
    {
        // Arrange
        var groceries = SeedCategory("Groceries");

        // Act
        var result = await _service.CreateAsync(new CreateLearnedRuleRequest(groceries.Id, "Whole Foods", "AB", null));

        // Assert
        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task CreateAsync_UnknownCategory_ReturnsFailure()
    {
        // Arrange
        var request = new CreateLearnedRuleRequest(Guid.NewGuid(), "Whole Foods", "WHOLEFDS MKT", null);

        // Act
        var result = await _service.CreateAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        (await _dbContext.LearnedRules.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task CreateAsync_ValidRequest_PersistsRuleWithNormalizedPattern()
    {
        // Arrange
        var groceries = SeedCategory("Groceries");
        var request = new CreateLearnedRuleRequest(groceries.Id, "  Whole Foods ", "wholefds   mkt", null);

        // Act
        var result = await _service.CreateAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        result.Response.Data!.Pattern.Should().Be("WHOLEFDS MKT");
        var stored = await _dbContext.LearnedRules.SingleAsync();
        stored.Pattern.Should().Be("WHOLEFDS MKT");
        stored.NormalizedVendor.Should().Be("Whole Foods");
        stored.CategoryId.Should().Be(groceries.Id);
    }

    [Fact]
    public async Task DeleteAsync_MissingRule_ReturnsNotFound()
    {
        // Arrange
        var missingId = Guid.NewGuid();

        // Act
        var result = await _service.DeleteAsync(missingId);

        // Assert
        result.IsNotFound.Should().BeTrue();
        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_RuleExists_RemovesRule()
    {
        // Arrange
        var groceries = SeedCategory("Groceries");
        var rule = SeedRule("WHOLEFDS MKT", "Whole Foods", groceries);

        // Act
        var result = await _service.DeleteAsync(rule.Id);

        // Assert
        result.Success.Should().BeTrue();
        (await _dbContext.LearnedRules.AnyAsync(r => r.Id == rule.Id)).Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_RuleReferencedByTransaction_ClearsReferenceAndKeepsAutoCategorizedFlag()
    {
        // Arrange
        var groceries = SeedCategory("Groceries");
        var rule = SeedRule("WHOLEFDS MKT", "Whole Foods", groceries);
        var transaction = CreateTransaction("WHOLEFDS MKT 10245");
        transaction.IsAutoCategorized = true;
        transaction.LearnedRuleId = rule.Id;
        transaction.CategoryId = groceries.Id;
        _dbContext.Transactions.Add(transaction);
        await _dbContext.SaveChangesAsync();

        // Act
        await _service.DeleteAsync(rule.Id);

        // Assert
        _dbContext.ChangeTracker.Clear();
        var stored = await _dbContext.Transactions.SingleAsync(t => t.Id == transaction.Id);
        stored.LearnedRuleId.Should().BeNull();
        stored.IsAutoCategorized.Should().BeTrue();
    }

    [Fact]
    public async Task GetByIdAsync_MissingRule_ReturnsNotFound()
    {
        // Arrange
        var missingId = Guid.NewGuid();

        // Act
        var result = await _service.GetByIdAsync(missingId);

        // Assert
        result.IsNotFound.Should().BeTrue();
    }

    [Fact]
    public async Task ListAsync_NoRules_ReturnsEmptyList()
    {
        // Arrange

        // Act
        var result = await _service.ListAsync();

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task ListAsync_TenRules_ReturnsPatternVendorAndCategory()
    {
        // Arrange
        var groceries = SeedCategory("Groceries");

        for (var index = 0; index < 10; index++)
        {
            SeedRule($"VENDOR {(char)('A' + index)}", $"Vendor {index}", groceries);
        }

        // Act
        var result = await _service.ListAsync();

        // Assert
        result.Data.Should().HaveCount(10);
        result.Data!.Should().OnlyContain(rule => rule.CategoryName == "Groceries");
        result.Data!.First().Pattern.Should().Be("VENDOR A");
        result.Data!.First().NormalizedVendor.Should().Be("Vendor 0");
    }

    [Fact]
    public async Task UpdateAsync_CategoryChanged_PersistsCategoryAndRefreshesUpdatedAt()
    {
        // Arrange
        var groceries = SeedCategory("Groceries");
        var household = SeedCategory("Household");
        var rule = SeedRule("WHOLEFDS MKT", "Whole Foods", groceries);
        var originalUpdatedAt = rule.UpdatedAt;

        // Act
        var result = await _service.UpdateAsync(rule.Id, new UpdateLearnedRuleRequest(household.Id, "Whole Foods", "WHOLEFDS MKT"));

        // Assert
        result.Success.Should().BeTrue();
        result.Response.Data!.CategoryName.Should().Be("Household");
        _dbContext.ChangeTracker.Clear();
        var stored = await _dbContext.LearnedRules.SingleAsync();
        stored.CategoryId.Should().Be(household.Id);
        stored.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }

    [Fact]
    public async Task UpdateAsync_CollidesWithAnotherRule_ReturnsFailure()
    {
        // Arrange
        var groceries = SeedCategory("Groceries");
        var first = SeedRule("WHOLEFDS MKT", "Whole Foods", groceries);
        SeedRule("TARGET", "Target", groceries);

        // Act
        var result = await _service.UpdateAsync(first.Id, new UpdateLearnedRuleRequest(groceries.Id, "Whole Foods", "target"));

        // Assert
        result.Success.Should().BeFalse();
        result.IsNotFound.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateAsync_MissingRule_ReturnsNotFound()
    {
        // Arrange
        var groceries = SeedCategory("Groceries");

        // Act
        var result = await _service.UpdateAsync(Guid.NewGuid(), new UpdateLearnedRuleRequest(groceries.Id, "Whole Foods", "WHOLEFDS MKT"));

        // Assert
        result.IsNotFound.Should().BeTrue();
    }

    private Transaction CreateTransaction(string description) =>
        new()
        {
            AccountId = _account.Id,
            Amount = 10.00m,
            Date = DateTime.UtcNow,
            Description = description,
            Id = Guid.NewGuid(),
            RawCsvRow = $"raw,{description}",
        };

    private Category SeedCategory(string name)
    {
        var category = new Category { Id = Guid.NewGuid(), Name = name };
        _dbContext.Categories.Add(category);
        _dbContext.SaveChanges();

        return category;
    }

    private LearnedRule SeedRule(string pattern, string vendor, Category category)
    {
        var timestamp = DateTime.UtcNow.AddDays(-1);
        var rule = new LearnedRule
        {
            CategoryId = category.Id,
            CreatedAt = timestamp,
            Id = Guid.NewGuid(),
            NormalizedVendor = vendor,
            Pattern = pattern,
            UpdatedAt = timestamp,
        };
        _dbContext.LearnedRules.Add(rule);
        _dbContext.SaveChanges();

        return rule;
    }
}
