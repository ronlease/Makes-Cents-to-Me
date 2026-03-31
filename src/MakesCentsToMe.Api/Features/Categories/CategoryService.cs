using MakesCentsToMe.Api.Common;
using MakesCentsToMe.Api.Infrastructure.Data;
using MakesCentsToMe.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace MakesCentsToMe.Api.Features.Categories;

public class CategoryService(AppDbContext dbContext) : ICategoryService
{
    public async Task<ApiResponse<CategoryResponse>> CreateAsync(CreateCategoryRequest request)
    {
        var duplicateExists = await dbContext.Categories
            .AnyAsync(c => c.Name == request.Name);

        if (duplicateExists)
        {
            return ApiResponse<CategoryResponse>.Fail($"A category named '{request.Name}' already exists.");
        }

        var category = new Category
        {
            Id = Guid.NewGuid(),
            IsDefault = false,
            Name = request.Name,
        };

        dbContext.Categories.Add(category);
        await dbContext.SaveChangesAsync();

        return ApiResponse<CategoryResponse>.Ok(MapToResponse(category, transactionCount: 0));
    }

    public async Task<ApiResponse<bool>> DeleteAsync(Guid id)
    {
        var category = await dbContext.Categories
            .Include(c => c.Transactions)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (category is null)
        {
            return ApiResponse<bool>.Fail($"Category '{id}' not found.");
        }

        if (category.Transactions.Count > 0)
        {
            return ApiResponse<bool>.Fail("Cannot delete a category that has assigned transactions.");
        }

        dbContext.Categories.Remove(category);
        await dbContext.SaveChangesAsync();

        return ApiResponse<bool>.Ok(true);
    }

    public async Task<ApiResponse<CategoryResponse>> GetByIdAsync(Guid id)
    {
        var category = await dbContext.Categories
            .Include(c => c.Transactions)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (category is null)
        {
            return ApiResponse<CategoryResponse>.Fail($"Category '{id}' not found.");
        }

        return ApiResponse<CategoryResponse>.Ok(MapToResponse(category, category.Transactions.Count));
    }

    public async Task<ApiResponse<IReadOnlyList<CategoryResponse>>> ListAsync()
    {
        var categories = await dbContext.Categories
            .OrderBy(c => c.Name)
            .Select(c => new CategoryResponse(c.Id, c.IsDefault, c.Name, c.Transactions.Count))
            .ToListAsync();

        return ApiResponse<IReadOnlyList<CategoryResponse>>.Ok(categories);
    }

    public async Task<ApiResponse<CategoryResponse>> UpdateAsync(Guid id, UpdateCategoryRequest request)
    {
        var category = await dbContext.Categories
            .Include(c => c.Transactions)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (category is null)
        {
            return ApiResponse<CategoryResponse>.Fail($"Category '{id}' not found.");
        }

        var duplicateExists = await dbContext.Categories
            .AnyAsync(c => c.Name == request.Name && c.Id != id);

        if (duplicateExists)
        {
            return ApiResponse<CategoryResponse>.Fail($"A category named '{request.Name}' already exists.");
        }

        category.Name = request.Name;
        await dbContext.SaveChangesAsync();

        return ApiResponse<CategoryResponse>.Ok(MapToResponse(category, category.Transactions.Count));
    }

    private static CategoryResponse MapToResponse(Category category, int transactionCount) =>
        new(category.Id, category.IsDefault, category.Name, transactionCount);
}
