using MakesCentsToMe.Api.Models.Entities;

namespace MakesCentsToMe.Api.Features.Accounts;

public record AccountResponse(Guid Id, string Name, AccountType AccountType, bool HasImportProfile);

public record CreateAccountRequest(string Name, AccountType AccountType);

public record UpdateAccountRequest(string Name, AccountType AccountType);
