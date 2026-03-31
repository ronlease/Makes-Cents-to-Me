using MakesCentsToMe.Api.Common;

namespace MakesCentsToMe.Api.Features.Institutions;

public interface IInstitutionService
{
    Task<ApiResponse<InstitutionResponse>> CreateAsync(CreateInstitutionRequest request);
    Task<ApiResponse<bool>> DeleteAsync(Guid id);
    Task<ApiResponse<InstitutionResponse>> GetByIdAsync(Guid id);
    Task<ApiResponse<IReadOnlyList<InstitutionResponse>>> ListAsync();
    Task<ApiResponse<InstitutionResponse>> UpdateAsync(Guid id, UpdateInstitutionRequest request);
}
