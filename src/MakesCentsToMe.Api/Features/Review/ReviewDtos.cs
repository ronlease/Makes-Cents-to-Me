using MakesCentsToMe.Api.Features.LearnedRules;

namespace MakesCentsToMe.Api.Features.Review;

public record OverrideTransactionRequest(Guid? CategoryId, string NormalizedVendor);

public record ReviewTransactionResponse(
    string AccountName,
    decimal Amount,
    Guid? CategoryId,
    decimal? Confidence,
    DateTime Date,
    string Description,
    Guid Id,
    string InstitutionName,
    bool IsAutoCategorized,
    LearnedRuleSuggestion? LearnedRuleSuggestion,
    string? NormalizedVendor,
    string? RawCategory,
    string Status,
    string? SuggestedCategory,
    Guid? SuggestedCategoryId,
    string? SuggestedNormalizedVendor);
