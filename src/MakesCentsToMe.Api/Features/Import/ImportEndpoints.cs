using MakesCentsToMe.Api.Common;
using Microsoft.AspNetCore.Mvc;

namespace MakesCentsToMe.Api.Features.Import;

public static class ImportEndpoints
{
    public static void MapImportEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/v1/accounts/{accountId:guid}/import")
            .WithTags("Import");

        group.MapPost("/upload", async (
            Guid accountId,
            IFormFile file,
            IImportService service) =>
        {
            if (file is null || file.Length == 0)
            {
                return Results.BadRequest(ApiResponse<UploadPreviewResponse>.Fail("No file provided."));
            }

            await using var stream = file.OpenReadStream();
            var result = await service.UploadPreviewAsync(accountId, stream);

            return result.Success
                ? Results.Ok(result)
                : Results.BadRequest(result);
        })
        .WithSummary("Upload a CSV file and preview its headers and first rows")
        .DisableAntiforgery()
        .Produces<ApiResponse<UploadPreviewResponse>>()
        .Produces<ApiResponse<UploadPreviewResponse>>(StatusCodes.Status400BadRequest);

        group.MapPost("/profile", async (
            Guid accountId,
            [FromBody] SaveImportProfileRequest request,
            IImportService service) =>
        {
            var result = await service.SaveProfileAsync(accountId, request);
            return result.Success
                ? Results.Created($"/api/v1/accounts/{accountId}/import/profile", result)
                : Results.BadRequest(result);
        })
        .WithSummary("Save an import profile (column mappings) for an account")
        .Produces<ApiResponse<ImportProfileResponse>>(StatusCodes.Status201Created)
        .Produces<ApiResponse<ImportProfileResponse>>(StatusCodes.Status400BadRequest);

        group.MapGet("/profile", async (Guid accountId, IImportService service) =>
        {
            var result = await service.GetProfileAsync(accountId);
            return result.Success
                ? Results.Ok(result)
                : Results.NotFound(result);
        })
        .WithSummary("Get the import profile for an account")
        .Produces<ApiResponse<ImportProfileResponse>>()
        .Produces<ApiResponse<ImportProfileResponse>>(StatusCodes.Status404NotFound);

        group.MapPut("/profile", async (
            Guid accountId,
            [FromBody] SaveImportProfileRequest request,
            IImportService service) =>
        {
            var result = await service.UpdateProfileAsync(accountId, request);
            return result.Success
                ? Results.Ok(result)
                : Results.NotFound(result);
        })
        .WithSummary("Update the import profile for an account")
        .Produces<ApiResponse<ImportProfileResponse>>()
        .Produces<ApiResponse<ImportProfileResponse>>(StatusCodes.Status404NotFound);

        group.MapPost("/process", async (
            Guid accountId,
            IFormFile file,
            [FromForm] decimal? openingBalance,
            [FromForm] decimal? closingBalance,
            IImportService service) =>
        {
            if (file is null || file.Length == 0)
            {
                return Results.BadRequest(ApiResponse<ProcessImportResponse>.Fail("No file provided."));
            }

            var request = new ProcessImportRequest(closingBalance, openingBalance);

            await using var stream = file.OpenReadStream();
            var result = await service.ProcessAsync(accountId, stream, request);

            return result.Success
                ? Results.Ok(result)
                : Results.BadRequest(result);
        })
        .WithSummary("Process a CSV file using the saved import profile and create transactions")
        .DisableAntiforgery()
        .Produces<ApiResponse<ProcessImportResponse>>()
        .Produces<ApiResponse<ProcessImportResponse>>(StatusCodes.Status400BadRequest);
    }
}
