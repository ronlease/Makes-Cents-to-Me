using MakesCentsToMe.Api.Common;
using MakesCentsToMe.Api.Infrastructure.Data;
using MakesCentsToMe.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace MakesCentsToMe.Api.Features.LearnedRules;

public class LearnedRuleService(AppDbContext dbContext) : ILearnedRuleService
{
    private const int MaximumLength = 500;
    private const int MinimumPatternLength = 3;

    public async Task<IReadOnlyList<Transaction>> ApplyRulesAsync(IReadOnlyList<Transaction> transactions)
    {
        var rules = await dbContext.LearnedRules
            .Include(learnedRule => learnedRule.Category)
            .ToListAsync();

        var unmatched = new List<Transaction>();

        foreach (var transaction in transactions)
        {
            var rule = LearnedRulePattern.FindBestMatch(rules, transaction.Description);

            if (rule is null)
            {
                unmatched.Add(transaction);
                continue;
            }

            transaction.CategoryId = rule.CategoryId;
            transaction.Confidence = 1.0m;
            transaction.IsAutoCategorized = true;
            transaction.LearnedRuleId = rule.Id;
            transaction.NormalizedVendor = rule.NormalizedVendor;
            transaction.Status = TransactionStatus.Committed;
            transaction.SuggestedCategory = rule.Category.Name;
            transaction.SuggestedCategoryId = rule.CategoryId;
            transaction.SuggestedNormalizedVendor = rule.NormalizedVendor;
        }

        return unmatched;
    }

    public async Task<LearnedRuleSuggestion?> BuildSuggestionAsync(Transaction transaction)
    {
        if (transaction.CategoryId is null || string.IsNullOrWhiteSpace(transaction.NormalizedVendor))
        {
            return null;
        }

        var categoryId = transaction.CategoryId.Value;
        var vendor = transaction.NormalizedVendor.Trim();

        var matchesClaudeSuggestion =
            transaction.SuggestedCategoryId == categoryId &&
            string.Equals(transaction.SuggestedNormalizedVendor?.Trim(), vendor, StringComparison.OrdinalIgnoreCase);

        if (matchesClaudeSuggestion)
        {
            return null;
        }

        var pattern = LearnedRulePattern.Derive(transaction.Description);

        if (pattern.Length < MinimumPatternLength)
        {
            return null;
        }

        var category = await dbContext.Categories
            .FirstOrDefaultAsync(c => c.Id == categoryId);

        if (category is null)
        {
            return null;
        }

        var existing = await dbContext.LearnedRules
            .FirstOrDefaultAsync(learnedRule => learnedRule.Pattern == pattern);

        if (existing is not null &&
            existing.CategoryId == categoryId &&
            string.Equals(existing.NormalizedVendor, vendor, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return new LearnedRuleSuggestion(categoryId, category.Name, existing?.Id, vendor, pattern, transaction.Id);
    }

    public async Task<LearnedRuleResult<LearnedRuleResponse>> CreateAsync(CreateLearnedRuleRequest request)
    {
        var pattern = LearnedRulePattern.Normalize(request.Pattern);
        var validationError = await ValidateAsync(pattern, request.NormalizedVendor, request.CategoryId, excludedId: null);

        if (validationError is not null)
        {
            return LearnedRuleResult<LearnedRuleResponse>.ValidationFailure(validationError);
        }

        var category = await dbContext.Categories.FirstAsync(c => c.Id == request.CategoryId);
        var now = DateTime.UtcNow;

        var learnedRule = new LearnedRule
        {
            Category = category,
            CategoryId = category.Id,
            CreatedAt = now,
            Id = Guid.NewGuid(),
            NormalizedVendor = request.NormalizedVendor.Trim(),
            Pattern = pattern,
            SourceTransactionId = request.SourceTransactionId,
            UpdatedAt = now,
        };

        dbContext.LearnedRules.Add(learnedRule);
        await dbContext.SaveChangesAsync();

        return LearnedRuleResult<LearnedRuleResponse>.Ok(MapToResponse(learnedRule));
    }

    public async Task<LearnedRuleResult<bool>> DeleteAsync(Guid id)
    {
        var learnedRule = await dbContext.LearnedRules.FirstOrDefaultAsync(r => r.Id == id);

        if (learnedRule is null)
        {
            return LearnedRuleResult<bool>.NotFound($"Learned rule '{id}' not found.");
        }

        var referencingTransactions = await dbContext.Transactions
            .Where(transaction => transaction.LearnedRuleId == id)
            .ToListAsync();

        foreach (var transaction in referencingTransactions)
        {
            transaction.LearnedRuleId = null;
        }

        dbContext.LearnedRules.Remove(learnedRule);
        await dbContext.SaveChangesAsync();

        return LearnedRuleResult<bool>.Ok(true);
    }

    public async Task<LearnedRuleResult<LearnedRuleResponse>> GetByIdAsync(Guid id)
    {
        var learnedRule = await dbContext.LearnedRules
            .Include(r => r.Category)
            .FirstOrDefaultAsync(r => r.Id == id);

        return learnedRule is null
            ? LearnedRuleResult<LearnedRuleResponse>.NotFound($"Learned rule '{id}' not found.")
            : LearnedRuleResult<LearnedRuleResponse>.Ok(MapToResponse(learnedRule));
    }

    public async Task<ApiResponse<IReadOnlyList<LearnedRuleResponse>>> ListAsync()
    {
        var learnedRules = await dbContext.LearnedRules
            .Include(r => r.Category)
            .OrderBy(r => r.Pattern)
            .ToListAsync();

        IReadOnlyList<LearnedRuleResponse> responses = learnedRules.Select(MapToResponse).ToList();

        return ApiResponse<IReadOnlyList<LearnedRuleResponse>>.Ok(responses);
    }

    public async Task<LearnedRuleResult<LearnedRuleResponse>> UpdateAsync(Guid id, UpdateLearnedRuleRequest request)
    {
        var learnedRule = await dbContext.LearnedRules
            .Include(r => r.Category)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (learnedRule is null)
        {
            return LearnedRuleResult<LearnedRuleResponse>.NotFound($"Learned rule '{id}' not found.");
        }

        var pattern = LearnedRulePattern.Normalize(request.Pattern);
        var validationError = await ValidateAsync(pattern, request.NormalizedVendor, request.CategoryId, id);

        if (validationError is not null)
        {
            return LearnedRuleResult<LearnedRuleResponse>.ValidationFailure(validationError);
        }

        var category = await dbContext.Categories.FirstAsync(c => c.Id == request.CategoryId);

        learnedRule.Category = category;
        learnedRule.CategoryId = category.Id;
        learnedRule.NormalizedVendor = request.NormalizedVendor.Trim();
        learnedRule.Pattern = pattern;
        learnedRule.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync();

        return LearnedRuleResult<LearnedRuleResponse>.Ok(MapToResponse(learnedRule));
    }

    private static LearnedRuleResponse MapToResponse(LearnedRule learnedRule) =>
        new(
            learnedRule.CategoryId,
            learnedRule.Category.Name,
            learnedRule.CreatedAt,
            learnedRule.Id,
            learnedRule.NormalizedVendor,
            learnedRule.Pattern,
            learnedRule.UpdatedAt);

    private async Task<string?> ValidateAsync(string normalizedPattern, string? normalizedVendor, Guid categoryId, Guid? excludedId)
    {
        if (string.IsNullOrWhiteSpace(normalizedPattern))
        {
            return "Pattern is required.";
        }

        if (string.IsNullOrWhiteSpace(normalizedVendor))
        {
            return "Normalized vendor is required.";
        }

        if (normalizedPattern.Length < MinimumPatternLength)
        {
            return $"Pattern must be at least {MinimumPatternLength} characters.";
        }

        if (normalizedPattern.Length > MaximumLength || normalizedVendor.Trim().Length > MaximumLength)
        {
            return $"Pattern and normalized vendor must be at most {MaximumLength} characters.";
        }

        var categoryExists = await dbContext.Categories.AnyAsync(c => c.Id == categoryId);

        if (!categoryExists)
        {
            return $"Category '{categoryId}' not found.";
        }

        var duplicateExists = await dbContext.LearnedRules
            .AnyAsync(r => r.Pattern == normalizedPattern && r.Id != excludedId);

        return duplicateExists
            ? $"A learned rule with pattern '{normalizedPattern}' already exists."
            : null;
    }
}
