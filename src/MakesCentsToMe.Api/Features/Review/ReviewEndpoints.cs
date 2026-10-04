using MakesCentsToMe.Api.Common;
using Microsoft.AspNetCore.Mvc;

namespace MakesCentsToMe.Api.Features.Review;

public static class ReviewEndpoints
{
    public static void MapReviewEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/v1/review")
            .WithTags("Review");

        group.MapGet("/", async (
            [FromQuery] Guid? accountId,
            IReviewService service) =>
        {
            var result = await service.ListPendingAsync(accountId);
            return Results.Ok(result);
        })
        .WithSummary("List transactions pending review")
        .WithDescription("Returns transactions awaiting review. Rows categorized by a learned rule before Claude analysis are flagged with isAutoCategorized.")
        .Produces<ApiResponse<IReadOnlyList<ReviewTransactionResponse>>>();

        group.MapPut("/{transactionId:guid}/accept", async (
            Guid transactionId,
            IReviewService service) =>
        {
            var result = await service.AcceptAsync(transactionId);
            return result.Success
                ? Results.Ok(result)
                : Results.BadRequest(result);
        })
        .WithSummary("Accept Claude's suggestion for a transaction")
        .Produces<ApiResponse<ReviewTransactionResponse>>()
        .Produces<ApiResponse<ReviewTransactionResponse>>(StatusCodes.Status400BadRequest);

        group.MapPost("/accept-all", async (
            [FromQuery] Guid? accountId,
            IReviewService service) =>
        {
            var result = await service.AcceptAllAsync(accountId);
            return Results.Ok(result);
        })
        .WithSummary("Accept all pending transactions")
        .Produces<ApiResponse<int>>();

        group.MapPut("/{transactionId:guid}/override", async (
            Guid transactionId,
            [FromBody] OverrideTransactionRequest request,
            IReviewService service) =>
        {
            var result = await service.OverrideAsync(transactionId, request);
            return result.Success
                ? Results.Ok(result)
                : Results.BadRequest(result);
        })
        .WithSummary("Override Claude's suggestion with user values")
        .WithDescription("Commits the user's vendor and category. When the correction differs from the existing suggestion, the response includes learnedRuleSuggestion, which the client can POST to /api/v1/learned-rules to promote it to a learned rule.")
        .Produces<ApiResponse<ReviewTransactionResponse>>()
        .Produces<ApiResponse<ReviewTransactionResponse>>(StatusCodes.Status400BadRequest);
    }
}
