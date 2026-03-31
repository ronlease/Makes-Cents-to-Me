using MakesCentsToMe.Api.Common;
using Microsoft.AspNetCore.Mvc;

namespace MakesCentsToMe.Api.Features.Institutions;

public static class InstitutionEndpoints
{
    public static void MapInstitutionEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/v1/institutions")
            .WithTags("Institutions");

        group.MapPost("/", async (
            [FromBody] CreateInstitutionRequest request,
            IInstitutionService service) =>
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return Results.BadRequest(ApiResponse<InstitutionResponse>.Fail("Name is required."));
            }

            var result = await service.CreateAsync(request);
            return result.Success
                ? Results.Created($"/api/v1/institutions/{result.Data!.Id}", result)
                : Results.BadRequest(result);
        })
        .WithSummary("Create a new institution")
        .Produces<ApiResponse<InstitutionResponse>>(StatusCodes.Status201Created)
        .Produces<ApiResponse<InstitutionResponse>>(StatusCodes.Status400BadRequest);

        group.MapGet("/", async (IInstitutionService service) =>
        {
            var result = await service.ListAsync();
            return Results.Ok(result);
        })
        .WithSummary("List all institutions")
        .Produces<ApiResponse<IReadOnlyList<InstitutionResponse>>>();

        group.MapGet("/{id:guid}", async (Guid id, IInstitutionService service) =>
        {
            var result = await service.GetByIdAsync(id);
            return result.Success
                ? Results.Ok(result)
                : Results.NotFound(result);
        })
        .WithSummary("Get an institution by ID")
        .Produces<ApiResponse<InstitutionResponse>>()
        .Produces<ApiResponse<InstitutionResponse>>(StatusCodes.Status404NotFound);

        group.MapPut("/{id:guid}", async (
            Guid id,
            [FromBody] UpdateInstitutionRequest request,
            IInstitutionService service) =>
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return Results.BadRequest(ApiResponse<InstitutionResponse>.Fail("Name is required."));
            }

            var result = await service.UpdateAsync(id, request);
            return result.Success
                ? Results.Ok(result)
                : Results.NotFound(result);
        })
        .WithSummary("Update an institution")
        .Produces<ApiResponse<InstitutionResponse>>()
        .Produces<ApiResponse<InstitutionResponse>>(StatusCodes.Status404NotFound);

        group.MapDelete("/{id:guid}", async (Guid id, IInstitutionService service) =>
        {
            var result = await service.DeleteAsync(id);
            return result.Success
                ? Results.NoContent()
                : Results.BadRequest(result);
        })
        .WithSummary("Delete an institution")
        .Produces(StatusCodes.Status204NoContent)
        .Produces<ApiResponse<bool>>(StatusCodes.Status400BadRequest);
    }
}
