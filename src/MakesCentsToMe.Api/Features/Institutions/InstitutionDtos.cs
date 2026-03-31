namespace MakesCentsToMe.Api.Features.Institutions;

public record CreateInstitutionRequest(string Name);

public record InstitutionResponse(Guid Id, string Name, int AccountCount);

public record UpdateInstitutionRequest(string Name);
