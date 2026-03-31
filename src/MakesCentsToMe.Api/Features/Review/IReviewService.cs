using MakesCentsToMe.Api.Common;

namespace MakesCentsToMe.Api.Features.Review;

public interface IReviewService
{
    Task<ApiResponse<ReviewTransactionResponse>> AcceptAsync(Guid transactionId);
    Task<ApiResponse<int>> AcceptAllAsync(Guid? accountId);
    Task<ApiResponse<IReadOnlyList<ReviewTransactionResponse>>> ListPendingAsync(Guid? accountId);
    Task<ApiResponse<ReviewTransactionResponse>> OverrideAsync(Guid transactionId, OverrideTransactionRequest request);
}
