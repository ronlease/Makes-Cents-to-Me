using MakesCentsToMe.Api.Common;

namespace MakesCentsToMe.Api.Features.Categories;

public interface ICategoryService
{
    Task<ApiResponse<CategoryResponse>> CreateAsync(CreateCategoryRequest request);
    Task<ApiResponse<bool>> DeleteAsync(Guid id);
    Task<ApiResponse<CategoryResponse>> GetByIdAsync(Guid id);
    Task<ApiResponse<IReadOnlyList<CategoryResponse>>> ListAsync();
    Task<ApiResponse<CategoryResponse>> UpdateAsync(Guid id, UpdateCategoryRequest request);
}
