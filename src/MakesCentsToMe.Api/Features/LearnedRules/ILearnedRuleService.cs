using MakesCentsToMe.Api.Common;
using MakesCentsToMe.Api.Models.Entities;

namespace MakesCentsToMe.Api.Features.LearnedRules;

public interface ILearnedRuleService
{
    /// <summary>Applies matching rules to the transactions and returns those that did not match. Does not save.</summary>
    Task<IReadOnlyList<Transaction>> ApplyRulesAsync(IReadOnlyList<Transaction> transactions);
    Task<LearnedRuleSuggestion?> BuildSuggestionAsync(Transaction transaction);
    Task<LearnedRuleResult<LearnedRuleResponse>> CreateAsync(CreateLearnedRuleRequest request);
    Task<LearnedRuleResult<bool>> DeleteAsync(Guid id);
    Task<LearnedRuleResult<LearnedRuleResponse>> GetByIdAsync(Guid id);
    Task<ApiResponse<IReadOnlyList<LearnedRuleResponse>>> ListAsync();
    Task<LearnedRuleResult<LearnedRuleResponse>> UpdateAsync(Guid id, UpdateLearnedRuleRequest request);
}
