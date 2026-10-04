using MakesCentsToMe.Api.Common;
using Microsoft.AspNetCore.Mvc;

namespace MakesCentsToMe.Api.Features.LearnedRules;

public static class LearnedRuleEndpoints
{
    public static void MapLearnedRuleEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/v1/learned-rules")
            .WithTags("Learned Rules");

        group.MapGet("/", async (ILearnedRuleService service) =>
        {
            var result = await service.ListAsync();
            return Results.Ok(result);
        })
        .WithSummary("List learned rules")
        .WithDescription("Returns all learned rules ordered by pattern.")
        .Produces<ApiResponse<IReadOnlyList<LearnedRuleResponse>>>();

        group.MapGet("/{id:guid}", async (Guid id, ILearnedRuleService service) =>
        {
            var result = await service.GetByIdAsync(id);
            return result.Success
                ? Results.Ok(result.Response)
                : Results.NotFound(result.Response);
        })
        .WithSummary("Get a learned rule by ID")
        .WithDescription("Returns a single learned rule, or 404 when it does not exist.")
        .Produces<ApiResponse<LearnedRuleResponse>>()
        .Produces<ApiResponse<LearnedRuleResponse>>(StatusCodes.Status404NotFound);

        group.MapPost("/", async (
            [FromBody] CreateLearnedRuleRequest request,
            ILearnedRuleService service) =>
        {
            var result = await service.CreateAsync(request);
            return result.Success
                ? Results.Created($"/api/v1/learned-rules/{result.Response.Data!.Id}", result.Response)
                : Results.BadRequest(result.Response);
        })
        .WithSummary("Create a learned rule")
        .WithDescription("Creates a rule mapping a normalized raw-description prefix to a vendor and category. Rejects blank or too-short patterns, unknown categories, and duplicate patterns.")
        .Produces<ApiResponse<LearnedRuleResponse>>(StatusCodes.Status201Created)
        .Produces<ApiResponse<LearnedRuleResponse>>(StatusCodes.Status400BadRequest);

        group.MapPut("/{id:guid}", async (
            Guid id,
            [FromBody] UpdateLearnedRuleRequest request,
            ILearnedRuleService service) =>
        {
            var result = await service.UpdateAsync(id, request);

            if (result.Success)
            {
                return Results.Ok(result.Response);
            }

            return result.IsNotFound
                ? Results.NotFound(result.Response)
                : Results.BadRequest(result.Response);
        })
        .WithSummary("Update a learned rule")
        .WithDescription("Updates the pattern, vendor, and category of a learned rule. Returns 404 when missing and 400 on validation failure.")
        .Produces<ApiResponse<LearnedRuleResponse>>()
        .Produces<ApiResponse<LearnedRuleResponse>>(StatusCodes.Status404NotFound)
        .Produces<ApiResponse<LearnedRuleResponse>>(StatusCodes.Status400BadRequest);

        group.MapDelete("/{id:guid}", async (Guid id, ILearnedRuleService service) =>
        {
            var result = await service.DeleteAsync(id);
            return result.Success
                ? Results.NoContent()
                : Results.NotFound(result.Response);
        })
        .WithSummary("Delete a learned rule")
        .WithDescription("Deletes a learned rule. Transactions it categorized keep their category and vendor but lose the rule link.")
        .Produces(StatusCodes.Status204NoContent)
        .Produces<ApiResponse<bool>>(StatusCodes.Status404NotFound);
    }
}
