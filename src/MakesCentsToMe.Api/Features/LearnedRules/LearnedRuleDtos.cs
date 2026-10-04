namespace MakesCentsToMe.Api.Features.LearnedRules;

public record CreateLearnedRuleRequest(Guid CategoryId, string NormalizedVendor, string Pattern, Guid? SourceTransactionId);

public record LearnedRuleResponse(
    Guid CategoryId,
    string CategoryName,
    DateTime CreatedAt,
    Guid Id,
    string NormalizedVendor,
    string Pattern,
    DateTime UpdatedAt);

public record LearnedRuleSuggestion(
    Guid CategoryId,
    string CategoryName,
    Guid? ExistingLearnedRuleId,
    string NormalizedVendor,
    string Pattern,
    Guid SourceTransactionId);

public record UpdateLearnedRuleRequest(Guid CategoryId, string NormalizedVendor, string Pattern);
