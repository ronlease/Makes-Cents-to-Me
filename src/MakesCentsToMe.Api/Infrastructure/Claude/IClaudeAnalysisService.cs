using MakesCentsToMe.Api.Models.Entities;

namespace MakesCentsToMe.Api.Infrastructure.Claude;

public interface IClaudeAnalysisService
{
    Task AnalyzeTransactionsAsync(List<Transaction> transactions);
}
