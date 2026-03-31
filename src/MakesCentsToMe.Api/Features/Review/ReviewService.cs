using MakesCentsToMe.Api.Common;
using MakesCentsToMe.Api.Infrastructure.Data;
using MakesCentsToMe.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace MakesCentsToMe.Api.Features.Review;

public class ReviewService(AppDbContext dbContext) : IReviewService
{
    public async Task<ApiResponse<ReviewTransactionResponse>> AcceptAsync(Guid transactionId)
    {
        var transaction = await dbContext.Transactions
            .Include(t => t.Account)
                .ThenInclude(a => a.Institution)
            .FirstOrDefaultAsync(t => t.Id == transactionId);

        if (transaction is null)
        {
            return ApiResponse<ReviewTransactionResponse>.Fail($"Transaction '{transactionId}' not found.");
        }

        if (transaction.Status != TransactionStatus.PendingReview)
        {
            return ApiResponse<ReviewTransactionResponse>.Fail(
                $"Transaction is in '{transaction.Status}' status and cannot be accepted.");
        }

        transaction.CategoryId = transaction.SuggestedCategoryId;
        transaction.NormalizedVendor = transaction.SuggestedNormalizedVendor;
        transaction.Status = TransactionStatus.Committed;

        await dbContext.SaveChangesAsync();

        return ApiResponse<ReviewTransactionResponse>.Ok(MapToResponse(transaction));
    }

    public async Task<ApiResponse<int>> AcceptAllAsync(Guid? accountId)
    {
        var query = dbContext.Transactions
            .Where(t => t.Status == TransactionStatus.PendingReview);

        if (accountId.HasValue)
        {
            query = query.Where(t => t.AccountId == accountId.Value);
        }

        var transactions = await query.ToListAsync();

        foreach (var transaction in transactions)
        {
            transaction.CategoryId = transaction.SuggestedCategoryId;
            transaction.NormalizedVendor = transaction.SuggestedNormalizedVendor;
            transaction.Status = TransactionStatus.Committed;
        }

        await dbContext.SaveChangesAsync();

        return ApiResponse<int>.Ok(transactions.Count);
    }

    public async Task<ApiResponse<IReadOnlyList<ReviewTransactionResponse>>> ListPendingAsync(Guid? accountId)
    {
        var query = dbContext.Transactions
            .Include(t => t.Account)
                .ThenInclude(a => a.Institution)
            .Where(t => t.Status == TransactionStatus.PendingReview
                     || t.Status == TransactionStatus.PendingAnalysis);

        if (accountId.HasValue)
        {
            query = query.Where(t => t.AccountId == accountId.Value);
        }

        var transactions = await query
            .OrderByDescending(t => t.Date)
            .ThenBy(t => t.Description)
            .ToListAsync();

        var responses = transactions.Select(MapToResponse).ToList();

        return ApiResponse<IReadOnlyList<ReviewTransactionResponse>>.Ok(responses);
    }

    public async Task<ApiResponse<ReviewTransactionResponse>> OverrideAsync(
        Guid transactionId,
        OverrideTransactionRequest request)
    {
        var transaction = await dbContext.Transactions
            .Include(t => t.Account)
                .ThenInclude(a => a.Institution)
            .FirstOrDefaultAsync(t => t.Id == transactionId);

        if (transaction is null)
        {
            return ApiResponse<ReviewTransactionResponse>.Fail($"Transaction '{transactionId}' not found.");
        }

        if (transaction.Status != TransactionStatus.PendingReview)
        {
            return ApiResponse<ReviewTransactionResponse>.Fail(
                $"Transaction is in '{transaction.Status}' status and cannot be overridden.");
        }

        if (request.CategoryId.HasValue)
        {
            var categoryExists = await dbContext.Categories.AnyAsync(c => c.Id == request.CategoryId.Value);
            if (!categoryExists)
            {
                return ApiResponse<ReviewTransactionResponse>.Fail($"Category '{request.CategoryId}' not found.");
            }
        }

        transaction.CategoryId = request.CategoryId;
        transaction.NormalizedVendor = request.NormalizedVendor;
        transaction.Status = TransactionStatus.Committed;

        await dbContext.SaveChangesAsync();

        return ApiResponse<ReviewTransactionResponse>.Ok(MapToResponse(transaction));
    }

    private static ReviewTransactionResponse MapToResponse(Transaction transaction) =>
        new(
            AccountName: transaction.Account.Name,
            Amount: transaction.Amount,
            CategoryId: transaction.CategoryId,
            Confidence: transaction.Confidence,
            Date: transaction.Date,
            Description: transaction.Description,
            Id: transaction.Id,
            InstitutionName: transaction.Account.Institution.Name,
            NormalizedVendor: transaction.NormalizedVendor,
            RawCategory: transaction.RawCategory,
            Status: transaction.Status.ToString(),
            SuggestedCategory: transaction.SuggestedCategory,
            SuggestedCategoryId: transaction.SuggestedCategoryId,
            SuggestedNormalizedVendor: transaction.SuggestedNormalizedVendor);
}
