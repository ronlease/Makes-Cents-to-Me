using MakesCentsToMe.Api.Common;
using MakesCentsToMe.Api.Infrastructure.Data;
using MakesCentsToMe.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace MakesCentsToMe.Api.Features.Institutions;

public interface IInstitutionService
{
    Task<ApiResponse<InstitutionResponse>> CreateAsync(CreateInstitutionRequest request);
    Task<ApiResponse<bool>> DeleteAsync(Guid id);
    Task<ApiResponse<InstitutionResponse>> GetByIdAsync(Guid id);
    Task<ApiResponse<IReadOnlyList<InstitutionResponse>>> ListAsync();
    Task<ApiResponse<InstitutionResponse>> UpdateAsync(Guid id, UpdateInstitutionRequest request);
}

public class InstitutionService(AppDbContext dbContext) : IInstitutionService
{
    public async Task<ApiResponse<InstitutionResponse>> CreateAsync(CreateInstitutionRequest request)
    {
        var institution = new Institution
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
        };

        dbContext.Institutions.Add(institution);
        await dbContext.SaveChangesAsync();

        return ApiResponse<InstitutionResponse>.Ok(MapToResponse(institution, 0));
    }

    public async Task<ApiResponse<bool>> DeleteAsync(Guid id)
    {
        var institution = await dbContext.Institutions
            .Include(i => i.Accounts)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (institution is null)
        {
            return ApiResponse<bool>.Fail($"Institution '{id}' not found.");
        }

        if (institution.Accounts.Count > 0)
        {
            return ApiResponse<bool>.Fail("Cannot delete an institution that has accounts.");
        }

        dbContext.Institutions.Remove(institution);
        await dbContext.SaveChangesAsync();

        return ApiResponse<bool>.Ok(true);
    }

    public async Task<ApiResponse<InstitutionResponse>> GetByIdAsync(Guid id)
    {
        var institution = await dbContext.Institutions
            .Include(i => i.Accounts)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (institution is null)
        {
            return ApiResponse<InstitutionResponse>.Fail($"Institution '{id}' not found.");
        }

        return ApiResponse<InstitutionResponse>.Ok(MapToResponse(institution, institution.Accounts.Count));
    }

    public async Task<ApiResponse<IReadOnlyList<InstitutionResponse>>> ListAsync()
    {
        var institutions = await dbContext.Institutions
            .Include(i => i.Accounts)
            .OrderBy(i => i.Name)
            .ToListAsync();

        var responses = institutions
            .Select(i => MapToResponse(i, i.Accounts.Count))
            .ToList();

        return ApiResponse<IReadOnlyList<InstitutionResponse>>.Ok(responses);
    }

    public async Task<ApiResponse<InstitutionResponse>> UpdateAsync(Guid id, UpdateInstitutionRequest request)
    {
        var institution = await dbContext.Institutions
            .Include(i => i.Accounts)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (institution is null)
        {
            return ApiResponse<InstitutionResponse>.Fail($"Institution '{id}' not found.");
        }

        institution.Name = request.Name;
        await dbContext.SaveChangesAsync();

        return ApiResponse<InstitutionResponse>.Ok(MapToResponse(institution, institution.Accounts.Count));
    }

    private static InstitutionResponse MapToResponse(Institution institution, int accountCount) =>
        new(institution.Id, institution.Name, accountCount);
}
