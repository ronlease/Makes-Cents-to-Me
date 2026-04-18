namespace MakesCentsToMe.Api.Features.Categories;

public record CategoryResponse(Guid Id, bool IsDefault, string Name, int TransactionCount);

public record CreateCategoryRequest(string Name);

public record UpdateCategoryRequest(string Name);
