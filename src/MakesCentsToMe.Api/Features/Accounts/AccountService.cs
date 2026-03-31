using MakesCentsToMe.Api.Common;
using MakesCentsToMe.Api.Infrastructure.Data;
using MakesCentsToMe.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace MakesCentsToMe.Api.Features.Accounts;

public class AccountService(AppDbContext dbContext) : IAccountService
{
    public async Task<ApiResponse<AccountResponse>> CreateAsync(Guid institutionId, CreateAccountRequest request)
    {
        var institutionExists = await dbContext.Institutions.AnyAsync(i => i.Id == institutionId);
        if (!institutionExists)
        {
            return ApiResponse<AccountResponse>.Fail($"Institution '{institutionId}' not found.");
        }

        var duplicateExists = await dbContext.Accounts
            .AnyAsync(a => a.InstitutionId == institutionId && a.Name == request.Name);

        if (duplicateExists)
        {
            return ApiResponse<AccountResponse>.Fail(
                $"An account named '{request.Name}' already exists in this institution.");
        }

        var account = new Account
        {
            AccountType = request.AccountType,
            Id = Guid.NewGuid(),
            InstitutionId = institutionId,
            Name = request.Name,
        };

        dbContext.Accounts.Add(account);
        await dbContext.SaveChangesAsync();

        return ApiResponse<AccountResponse>.Ok(MapToResponse(account, hasImportProfile: false));
    }

    public async Task<ApiResponse<bool>> DeleteAsync(Guid institutionId, Guid id)
    {
        var account = await dbContext.Accounts
            .Include(a => a.Transactions)
            .FirstOrDefaultAsync(a => a.Id == id && a.InstitutionId == institutionId);

        if (account is null)
        {
            return ApiResponse<bool>.Fail($"Account '{id}' not found.");
        }

        if (account.Transactions.Count > 0)
        {
            return ApiResponse<bool>.Fail("Cannot delete an account that has transactions.");
        }

        dbContext.Accounts.Remove(account);
        await dbContext.SaveChangesAsync();

        return ApiResponse<bool>.Ok(true);
    }

    public async Task<ApiResponse<AccountResponse>> GetByIdAsync(Guid institutionId, Guid id)
    {
        var account = await dbContext.Accounts
            .Include(a => a.ImportProfile)
            .FirstOrDefaultAsync(a => a.Id == id && a.InstitutionId == institutionId);

        if (account is null)
        {
            return ApiResponse<AccountResponse>.Fail($"Account '{id}' not found.");
        }

        return ApiResponse<AccountResponse>.Ok(MapToResponse(account, account.ImportProfile is not null));
    }

    public async Task<ApiResponse<IReadOnlyList<AccountResponse>>> ListAsync(Guid institutionId)
    {
        var institutionExists = await dbContext.Institutions.AnyAsync(i => i.Id == institutionId);
        if (!institutionExists)
        {
            return ApiResponse<IReadOnlyList<AccountResponse>>.Fail($"Institution '{institutionId}' not found.");
        }

        var accounts = await dbContext.Accounts
            .Include(a => a.ImportProfile)
            .Where(a => a.InstitutionId == institutionId)
            .OrderBy(a => a.Name)
            .ToListAsync();

        var responses = accounts
            .Select(a => MapToResponse(a, a.ImportProfile is not null))
            .ToList();

        return ApiResponse<IReadOnlyList<AccountResponse>>.Ok(responses);
    }

    public async Task<ApiResponse<AccountResponse>> UpdateAsync(Guid institutionId, Guid id, UpdateAccountRequest request)
    {
        var account = await dbContext.Accounts
            .Include(a => a.ImportProfile)
            .FirstOrDefaultAsync(a => a.Id == id && a.InstitutionId == institutionId);

        if (account is null)
        {
            return ApiResponse<AccountResponse>.Fail($"Account '{id}' not found.");
        }

        var duplicateExists = await dbContext.Accounts
            .AnyAsync(a => a.InstitutionId == institutionId && a.Name == request.Name && a.Id != id);

        if (duplicateExists)
        {
            return ApiResponse<AccountResponse>.Fail(
                $"An account named '{request.Name}' already exists in this institution.");
        }

        account.AccountType = request.AccountType;
        account.Name = request.Name;

        await dbContext.SaveChangesAsync();

        return ApiResponse<AccountResponse>.Ok(MapToResponse(account, account.ImportProfile is not null));
    }

    private static AccountResponse MapToResponse(Account account, bool hasImportProfile) =>
        new(account.Id, account.Name, account.AccountType, hasImportProfile);
}
