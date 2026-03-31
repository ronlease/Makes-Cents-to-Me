using MakesCentsToMe.Api.Common;
using Microsoft.AspNetCore.Mvc;

namespace MakesCentsToMe.Api.Features.Categories;

public static class CategoryEndpoints
{
    public static void MapCategoryEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/v1/categories")
            .WithTags("Categories");

        group.MapPost("/", async (
            [FromBody] CreateCategoryRequest request,
            ICategoryService service) =>
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return Results.BadRequest(ApiResponse<CategoryResponse>.Fail("Name is required."));
            }

            var result = await service.CreateAsync(request);
            return result.Success
                ? Results.Created($"/api/v1/categories/{result.Data!.Id}", result)
                : Results.BadRequest(result);
        })
        .WithSummary("Create a new category")
        .Produces<ApiResponse<CategoryResponse>>(StatusCodes.Status201Created)
        .Produces<ApiResponse<CategoryResponse>>(StatusCodes.Status400BadRequest);

        group.MapGet("/", async (ICategoryService service) =>
        {
            var result = await service.ListAsync();
            return Results.Ok(result);
        })
        .WithSummary("List all categories")
        .Produces<ApiResponse<IReadOnlyList<CategoryResponse>>>();

        group.MapGet("/{id:guid}", async (Guid id, ICategoryService service) =>
        {
            var result = await service.GetByIdAsync(id);
            return result.Success
                ? Results.Ok(result)
                : Results.NotFound(result);
        })
        .WithSummary("Get a category by ID")
        .Produces<ApiResponse<CategoryResponse>>()
        .Produces<ApiResponse<CategoryResponse>>(StatusCodes.Status404NotFound);

        group.MapPut("/{id:guid}", async (
            Guid id,
            [FromBody] UpdateCategoryRequest request,
            ICategoryService service) =>
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return Results.BadRequest(ApiResponse<CategoryResponse>.Fail("Name is required."));
            }

            var result = await service.UpdateAsync(id, request);
            return result.Success
                ? Results.Ok(result)
                : Results.NotFound(result);
        })
        .WithSummary("Update a category")
        .Produces<ApiResponse<CategoryResponse>>()
        .Produces<ApiResponse<CategoryResponse>>(StatusCodes.Status404NotFound);

        group.MapDelete("/{id:guid}", async (Guid id, ICategoryService service) =>
        {
            var result = await service.DeleteAsync(id);
            return result.Success
                ? Results.NoContent()
                : Results.BadRequest(result);
        })
        .WithSummary("Delete a category")
        .Produces(StatusCodes.Status204NoContent)
        .Produces<ApiResponse<bool>>(StatusCodes.Status400BadRequest);
    }
}
