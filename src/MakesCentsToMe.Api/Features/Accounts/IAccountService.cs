using MakesCentsToMe.Api.Common;

namespace MakesCentsToMe.Api.Features.Accounts;

public interface IAccountService
{
    Task<ApiResponse<AccountResponse>> CreateAsync(Guid institutionId, CreateAccountRequest request);
    Task<ApiResponse<bool>> DeleteAsync(Guid institutionId, Guid id);
    Task<ApiResponse<AccountResponse>> GetByIdAsync(Guid institutionId, Guid id);
    Task<ApiResponse<IReadOnlyList<AccountResponse>>> ListAsync(Guid institutionId);
    Task<ApiResponse<AccountResponse>> UpdateAsync(Guid institutionId, Guid id, UpdateAccountRequest request);
}
