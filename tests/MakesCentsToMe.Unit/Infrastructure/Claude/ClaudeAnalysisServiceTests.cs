// Feature: Claude Analysis — AnalyzeTransactionsAsync
//
// Scenario: Empty transaction list short-circuits
//   Given an empty list of transactions
//   When AnalyzeTransactionsAsync is called
//   Then no HTTP request is made
//
// Scenario: Successful response applies normalized vendor, category, and confidence
//   Given one transaction and a successful Claude response mapping index 0 to a known category
//   When AnalyzeTransactionsAsync is called
//   Then the transaction status becomes PendingReview
//   And SuggestedNormalizedVendor, SuggestedCategory, SuggestedCategoryId, Confidence are populated
//
// Scenario: Suggested category id is null when category name is not in the database
//   Given a transaction and a response referencing a category name that does not exist
//   When AnalyzeTransactionsAsync is called
//   Then SuggestedCategory is still set but SuggestedCategoryId remains null
//
// Scenario: Category name matching is case-insensitive
//   Given a transaction and a response whose category name differs only by case from a known category
//   When AnalyzeTransactionsAsync is called
//   Then SuggestedCategoryId is populated with the matching category's id
//
// Scenario: Out-of-range result index is ignored
//   Given a transaction and a response whose result references an index outside the batch
//   When AnalyzeTransactionsAsync is called
//   Then the transaction is left unchanged
//
// Scenario: All retries exhausted marks the batch as PendingAnalysis
//   Given the Claude API returns 500 on every attempt
//   When AnalyzeTransactionsAsync is called
//   Then every transaction in the batch has status PendingAnalysis
//
// Scenario: Transactions exceeding batch size are split across multiple HTTP requests
//   Given 51 transactions and a successful response for each batch
//   When AnalyzeTransactionsAsync is called
//   Then two HTTP requests are made (one per batch)

using FluentAssertions;
using MakesCentsToMe.Api.Infrastructure.Claude;
using MakesCentsToMe.Api.Infrastructure.Data;
using MakesCentsToMe.Api.Models.Entities;
using MakesCentsToMe.Unit.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using System.Net;
using System.Text;
using System.Text.Json;

namespace MakesCentsToMe.Unit.Infrastructure.Claude;

public class ClaudeAnalysisServiceTests : IDisposable
{
    private readonly AppDbContext _dbContext;
    private readonly StubHttpMessageHandler _handler;
    private readonly HttpClient _httpClient;
    private readonly ClaudeAnalysisService _service;

    public ClaudeAnalysisServiceTests()
    {
        _dbContext = InMemoryDbContextFactory.Create();
        _handler = new StubHttpMessageHandler();
        _httpClient = new HttpClient(_handler) { BaseAddress = new Uri("https://api.anthropic.test/") };
        _service = new ClaudeAnalysisService(_dbContext, _httpClient, NullLogger<ClaudeAnalysisService>.Instance);
    }

    public void Dispose()
    {
        _httpClient.Dispose();
        _handler.Dispose();
        _dbContext.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task AnalyzeTransactionsAsync_EmptyList_DoesNotCallClaude()
    {
        await _service.AnalyzeTransactionsAsync([]);

        _handler.CallCount.Should().Be(0);
    }

    [Fact]
    public async Task AnalyzeTransactionsAsync_SuccessfulResponse_AppliesAnalysisToTransaction()
    {
        var groceriesId = await SeedCategoryAsync("Groceries");
        var transaction = CreateTransaction("WHOLEFDS MRKT 12345");

        _handler.EnqueueJsonTextResponse(new[] { new { index = 0, normalizedVendor = "Whole Foods", suggestedCategory = "Groceries", confidence = 0.95m } });

        await _service.AnalyzeTransactionsAsync([transaction]);

        transaction.Status.Should().Be(TransactionStatus.PendingReview);
        transaction.SuggestedNormalizedVendor.Should().Be("Whole Foods");
        transaction.SuggestedCategory.Should().Be("Groceries");
        transaction.SuggestedCategoryId.Should().Be(groceriesId);
        transaction.Confidence.Should().Be(0.95m);
        _handler.CallCount.Should().Be(1);
    }

    [Fact]
    public async Task AnalyzeTransactionsAsync_UnknownCategoryName_LeavesSuggestedCategoryIdNull()
    {
        await SeedCategoryAsync("Groceries");
        var transaction = CreateTransaction("AMAZON.COM*1Z0");

        _handler.EnqueueJsonTextResponse(new[] { new { index = 0, normalizedVendor = "Amazon", suggestedCategory = "Shopping", confidence = 0.7m } });

        await _service.AnalyzeTransactionsAsync([transaction]);

        transaction.SuggestedCategory.Should().Be("Shopping");
        transaction.SuggestedCategoryId.Should().BeNull();
    }

    [Fact]
    public async Task AnalyzeTransactionsAsync_CategoryNameDifferentCasing_MapsToCategoryId()
    {
        var groceriesId = await SeedCategoryAsync("Groceries");
        var transaction = CreateTransaction("KROGER #42");

        _handler.EnqueueJsonTextResponse(new[] { new { index = 0, normalizedVendor = "Kroger", suggestedCategory = "groceries", confidence = 0.9m } });

        await _service.AnalyzeTransactionsAsync([transaction]);

        transaction.SuggestedCategoryId.Should().Be(groceriesId);
    }

    [Fact]
    public async Task AnalyzeTransactionsAsync_OutOfRangeIndex_LeavesTransactionUnchanged()
    {
        await SeedCategoryAsync("Groceries");
        var transaction = CreateTransaction("TRADER JOES #1");
        var originalStatus = transaction.Status;

        _handler.EnqueueJsonTextResponse(new[] { new { index = 99, normalizedVendor = "Ignored", suggestedCategory = "Groceries", confidence = 0.5m } });

        await _service.AnalyzeTransactionsAsync([transaction]);

        transaction.Status.Should().Be(originalStatus);
        transaction.SuggestedNormalizedVendor.Should().BeNull();
        transaction.SuggestedCategory.Should().BeNull();
    }

    [Fact]
    public async Task AnalyzeTransactionsAsync_AllRetriesFail_MarksBatchAsPendingAnalysis()
    {
        await SeedCategoryAsync("Groceries");
        var transaction = CreateTransaction("SOMETHING");

        _handler.EnqueueErrorResponse(HttpStatusCode.InternalServerError);
        _handler.EnqueueErrorResponse(HttpStatusCode.InternalServerError);
        _handler.EnqueueErrorResponse(HttpStatusCode.InternalServerError);
        _handler.EnqueueErrorResponse(HttpStatusCode.InternalServerError);

        await _service.AnalyzeTransactionsAsync([transaction]);

        transaction.Status.Should().Be(TransactionStatus.PendingAnalysis);
        _handler.CallCount.Should().Be(4);
    }

    [Fact]
    public async Task AnalyzeTransactionsAsync_FirstAttemptFailsSecondSucceeds_AppliesResultOnRetry()
    {
        await SeedCategoryAsync("Groceries");
        var transaction = CreateTransaction("ANY");

        _handler.EnqueueErrorResponse(HttpStatusCode.InternalServerError);
        _handler.EnqueueJsonTextResponse(new[] { new { index = 0, normalizedVendor = "Retry Vendor", suggestedCategory = "Groceries", confidence = 0.8m } });

        await _service.AnalyzeTransactionsAsync([transaction]);

        transaction.Status.Should().Be(TransactionStatus.PendingReview);
        transaction.SuggestedNormalizedVendor.Should().Be("Retry Vendor");
        _handler.CallCount.Should().Be(2);
    }

    [Fact]
    public async Task AnalyzeTransactionsAsync_MoreThanBatchSize_SplitsIntoMultipleRequests()
    {
        await SeedCategoryAsync("Groceries");
        var transactions = Enumerable.Range(0, 51).Select(index => CreateTransaction($"T{index}")).ToList();

        _handler.EnqueueJsonTextResponse(
            Enumerable.Range(0, 50)
                .Select(index => new { index, normalizedVendor = $"V{index}", suggestedCategory = "Groceries", confidence = 0.9m })
                .ToArray());
        _handler.EnqueueJsonTextResponse(new[] { new { index = 50, normalizedVendor = "V50", suggestedCategory = "Groceries", confidence = 0.9m } });

        await _service.AnalyzeTransactionsAsync(transactions);

        _handler.CallCount.Should().Be(2);
        transactions.Should().OnlyContain(t => t.Status == TransactionStatus.PendingReview);
        transactions[50].SuggestedNormalizedVendor.Should().Be("V50");
    }

    private static Transaction CreateTransaction(string description) => new()
    {
        AccountId = Guid.NewGuid(),
        Amount = 10m,
        Date = DateTime.UtcNow,
        Description = description,
        Id = Guid.NewGuid(),
    };

    private async Task<Guid> SeedCategoryAsync(string name)
    {
        var category = new Category { Id = Guid.NewGuid(), Name = name };
        _dbContext.Categories.Add(category);
        await _dbContext.SaveChangesAsync();
        return category.Id;
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        private readonly Queue<HttpResponseMessage> _responses = new();

        public int CallCount { get; private set; }

        public void EnqueueErrorResponse(HttpStatusCode statusCode) =>
            _responses.Enqueue(new HttpResponseMessage(statusCode) { Content = new StringContent("{}", Encoding.UTF8, "application/json") });

        public void EnqueueJsonTextResponse(object payload)
        {
            var textBlockBody = JsonSerializer.Serialize(payload, JsonOptions);
            var envelope = new { content = new[] { new { type = "text", text = textBlockBody } } };
            var envelopeJson = JsonSerializer.Serialize(envelope, JsonOptions);
            _responses.Enqueue(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(envelopeJson, Encoding.UTF8, "application/json"),
            });
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CallCount++;
            if (_responses.Count == 0)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError));
            }
            return Task.FromResult(_responses.Dequeue());
        }
    }
}
