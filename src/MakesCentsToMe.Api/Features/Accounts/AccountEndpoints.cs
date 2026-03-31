using MakesCentsToMe.Api.Common;
using Microsoft.AspNetCore.Mvc;

namespace MakesCentsToMe.Api.Features.Accounts;

public static class AccountEndpoints
{
    public static void MapAccountEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/v1/institutions/{institutionId:guid}/accounts")
            .WithTags("Accounts");

        group.MapPost("/", async (
            Guid institutionId,
            [FromBody] CreateAccountRequest request,
            IAccountService service) =>
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return Results.BadRequest(ApiResponse<AccountResponse>.Fail("Name is required."));
            }

            var result = await service.CreateAsync(institutionId, request);
            return result.Success
                ? Results.Created($"/api/v1/institutions/{institutionId}/accounts/{result.Data!.Id}", result)
                : Results.BadRequest(result);
        })
        .WithSummary("Create a new account within an institution")
        .Produces<ApiResponse<AccountResponse>>(StatusCodes.Status201Created)
        .Produces<ApiResponse<AccountResponse>>(StatusCodes.Status400BadRequest);

        group.MapGet("/", async (Guid institutionId, IAccountService service) =>
        {
            var result = await service.ListAsync(institutionId);
            return result.Success
                ? Results.Ok(result)
                : Results.NotFound(result);
        })
        .WithSummary("List all accounts for an institution")
        .Produces<ApiResponse<IReadOnlyList<AccountResponse>>>()
        .Produces<ApiResponse<IReadOnlyList<AccountResponse>>>(StatusCodes.Status404NotFound);

        group.MapGet("/{id:guid}", async (Guid institutionId, Guid id, IAccountService service) =>
        {
            var result = await service.GetByIdAsync(institutionId, id);
            return result.Success
                ? Results.Ok(result)
                : Results.NotFound(result);
        })
        .WithSummary("Get an account by ID")
        .Produces<ApiResponse<AccountResponse>>()
        .Produces<ApiResponse<AccountResponse>>(StatusCodes.Status404NotFound);

        group.MapPut("/{id:guid}", async (
            Guid institutionId,
            Guid id,
            [FromBody] UpdateAccountRequest request,
            IAccountService service) =>
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return Results.BadRequest(ApiResponse<AccountResponse>.Fail("Name is required."));
            }

            var result = await service.UpdateAsync(institutionId, id, request);
            return result.Success
                ? Results.Ok(result)
                : Results.NotFound(result);
        })
        .WithSummary("Update an account")
        .Produces<ApiResponse<AccountResponse>>()
        .Produces<ApiResponse<AccountResponse>>(StatusCodes.Status404NotFound);

        group.MapDelete("/{id:guid}", async (Guid institutionId, Guid id, IAccountService service) =>
        {
            var result = await service.DeleteAsync(institutionId, id);
            return result.Success
                ? Results.NoContent()
                : Results.BadRequest(result);
        })
        .WithSummary("Delete an account")
        .Produces(StatusCodes.Status204NoContent)
        .Produces<ApiResponse<bool>>(StatusCodes.Status400BadRequest);
    }
}
