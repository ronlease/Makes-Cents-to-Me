using MakesCentsToMe.Api.Common;

namespace MakesCentsToMe.Api.Features.Import;

public interface IImportService
{
    Task<ApiResponse<ImportProfileResponse>> GetProfileAsync(Guid accountId);
    Task<ApiResponse<ProcessImportResponse>> ProcessAsync(Guid accountId, Stream csvStream, ProcessImportRequest request);
    Task<ApiResponse<ImportProfileResponse>> SaveProfileAsync(Guid accountId, SaveImportProfileRequest request);
    Task<ApiResponse<UploadPreviewResponse>> UploadPreviewAsync(Guid accountId, Stream csvStream);
    Task<ApiResponse<ImportProfileResponse>> UpdateProfileAsync(Guid accountId, SaveImportProfileRequest request);
}
