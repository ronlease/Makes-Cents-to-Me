using MakesCentsToMe.Api.Infrastructure.Data;
using MakesCentsToMe.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;

namespace MakesCentsToMe.Api.Infrastructure.Claude;

public class ClaudeAnalysisService(
    AppDbContext dbContext,
    HttpClient httpClient,
    ILogger<ClaudeAnalysisService> logger) : IClaudeAnalysisService
{
    private const int BatchSize = 50;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    private const string SystemPrompt = """
        You are a financial transaction analyzer. Given a list of raw transaction descriptions from bank/credit card statements,
        you will normalize each vendor name and suggest a spending category.

        Rules:
        - normalizedVendor: Clean merchant name. Remove transaction codes, locations, dates, card numbers.
          Example: "AMZN MKTP US*2K1ABC1Z0" → "Amazon", "WAL-MART #1234 SPRINGFIELD IL" → "Walmart"
        - suggestedCategory: Must be exactly one of the provided category names. Choose the best match.
        - confidence: A decimal from 0.0 to 1.0 indicating how confident you are in both the vendor and category.

        Respond with ONLY a JSON array. No markdown, no explanation. Each element must have:
        { "index": <int>, "normalizedVendor": "<string>", "suggestedCategory": "<string>", "confidence": <decimal> }
        """;

    public async Task AnalyzeTransactionsAsync(List<Transaction> transactions)
    {
        if (transactions.Count == 0) return;

        var categories = await dbContext.Categories
            .OrderBy(c => c.Name)
            .Select(c => c.Name)
            .ToListAsync();

        var categoryLookup = await dbContext.Categories
            .ToDictionaryAsync(c => c.Name, c => c.Id, StringComparer.OrdinalIgnoreCase);

        for (var batchStart = 0; batchStart < transactions.Count; batchStart += BatchSize)
        {
            var batch = transactions.Skip(batchStart).Take(BatchSize).ToList();
            await AnalyzeBatchAsync(batch, batchStart, categories, categoryLookup);
        }
    }

    private async Task AnalyzeBatchAsync(
        List<Transaction> batch,
        int globalOffset,
        List<string> categories,
        Dictionary<string, Guid> categoryLookup)
    {
        var userMessage = BuildUserMessage(batch, globalOffset, categories);

        var request = new ClaudeMessageRequest
        {
            Messages = [new ClaudeRequestMessage { Content = userMessage, Role = "user" }],
            System = SystemPrompt,
        };

        try
        {
            var results = await SendRequestWithRetryAsync(request);
            ApplyResults(batch, globalOffset, results, categoryLookup);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Claude analysis failed for batch starting at {Offset}. Marking as PendingAnalysis.", globalOffset);
            foreach (var transaction in batch)
            {
                transaction.Status = TransactionStatus.PendingAnalysis;
            }
        }
    }

    private static string BuildUserMessage(List<Transaction> batch, int globalOffset, List<string> categories)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Categories: " + string.Join(", ", categories));
        builder.AppendLine();
        builder.AppendLine("Transactions:");

        for (var transactionIndex = 0; transactionIndex < batch.Count; transactionIndex++)
        {
            builder.AppendLine($"  {globalOffset + transactionIndex}: \"{batch[transactionIndex].Description}\"");
        }

        return builder.ToString();
    }

    private static void ApplyResults(
        List<Transaction> batch,
        int globalOffset,
        List<TransactionAnalysisResult> results,
        Dictionary<string, Guid> categoryLookup)
    {
        foreach (var result in results)
        {
            var localIndex = result.Index - globalOffset;
            if (localIndex < 0 || localIndex >= batch.Count) continue;

            var transaction = batch[localIndex];
            transaction.Confidence = result.Confidence;
            transaction.Status = TransactionStatus.PendingReview;
            transaction.SuggestedCategory = result.SuggestedCategory;
            transaction.SuggestedNormalizedVendor = result.NormalizedVendor;

            if (categoryLookup.TryGetValue(result.SuggestedCategory, out var categoryId))
            {
                transaction.SuggestedCategoryId = categoryId;
            }
        }
    }

    private async Task<List<TransactionAnalysisResult>> SendRequestWithRetryAsync(ClaudeMessageRequest request)
    {
        var delays = new[] { 1000, 2000, 4000 };

        for (var attempt = 0; attempt <= delays.Length; attempt++)
        {
            try
            {
                var json = JsonSerializer.Serialize(request, JsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync("v1/messages", content);
                response.EnsureSuccessStatusCode();

                var responseBody = await response.Content.ReadAsStringAsync();
                var claudeResponse = JsonSerializer.Deserialize<ClaudeMessageResponse>(responseBody, JsonOptions);

                var textBlock = claudeResponse?.Content.FirstOrDefault(c => c.Type == "text");
                if (textBlock?.Text is null)
                {
                    throw new InvalidOperationException("Claude returned no text content.");
                }

                var results = JsonSerializer.Deserialize<List<TransactionAnalysisResult>>(textBlock.Text, JsonOptions);
                return results ?? [];
            }
            catch when (attempt < delays.Length)
            {
                logger.LogWarning("Claude API attempt {Attempt} failed. Retrying in {Delay}ms.", attempt + 1, delays[attempt]);
                await Task.Delay(delays[attempt]);
            }
        }

        return [];
    }
}
