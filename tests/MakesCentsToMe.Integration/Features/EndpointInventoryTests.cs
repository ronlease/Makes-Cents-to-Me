using FluentAssertions;
using MakesCentsToMe.Integration.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.Extensions.DependencyInjection;

namespace MakesCentsToMe.Integration.Features;

// Feature: Endpoint inventory guard
//
// Scenario: Every /api/v1 route has a contract test
//   Given the API host has started
//   When the registered /api/v1 endpoints are enumerated
//   Then they match the (HTTP method, route pattern) pairs covered by the endpoint contract tests
//   And adding or removing an endpoint fails this test until the contract tests are updated
[Collection(IntegrationTestCollection.Name)]
public class EndpointInventoryTests(IntegrationTestWebApplicationFactory factory)
{
    private static readonly string[] CoveredEndpoints =
    [
        "DELETE api/v1/categories/{id:guid}",
        "DELETE api/v1/institutions/{id:guid}",
        "DELETE api/v1/institutions/{institutionId:guid}/accounts/{id:guid}",
        "DELETE api/v1/learned-rules/{id:guid}",
        "GET api/v1/accounts/{accountId:guid}/import/profile",
        "GET api/v1/categories",
        "GET api/v1/categories/{id:guid}",
        "GET api/v1/institutions",
        "GET api/v1/institutions/{id:guid}",
        "GET api/v1/institutions/{institutionId:guid}/accounts",
        "GET api/v1/institutions/{institutionId:guid}/accounts/{id:guid}",
        "GET api/v1/learned-rules",
        "GET api/v1/learned-rules/{id:guid}",
        "GET api/v1/review",
        "POST api/v1/accounts/{accountId:guid}/import/process",
        "POST api/v1/accounts/{accountId:guid}/import/profile",
        "POST api/v1/accounts/{accountId:guid}/import/upload",
        "POST api/v1/categories",
        "POST api/v1/institutions",
        "POST api/v1/institutions/{institutionId:guid}/accounts",
        "POST api/v1/learned-rules",
        "POST api/v1/review/accept-all",
        "PUT api/v1/accounts/{accountId:guid}/import/profile",
        "PUT api/v1/categories/{id:guid}",
        "PUT api/v1/institutions/{id:guid}",
        "PUT api/v1/institutions/{institutionId:guid}/accounts/{id:guid}",
        "PUT api/v1/learned-rules/{id:guid}",
        "PUT api/v1/review/{transactionId:guid}/accept",
        "PUT api/v1/review/{transactionId:guid}/override",
    ];

    [Fact]
    public void EndpointInventory_AllApiV1Routes_AreCoveredByContractTests()
    {
        // Arrange
        var endpointDataSource = factory.Services.GetRequiredService<EndpointDataSource>();

        // Act
        var registered = endpointDataSource.Endpoints
            .OfType<RouteEndpoint>()
            .Select(endpoint => (Pattern: NormalizePattern(endpoint.RoutePattern), Methods: endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()?.HttpMethods))
            .Where(endpoint => endpoint.Pattern.StartsWith("api/v1", StringComparison.OrdinalIgnoreCase))
            .SelectMany(endpoint => (endpoint.Methods ?? []).Select(method => $"{method} {endpoint.Pattern}"))
            .OrderBy(entry => entry, StringComparer.Ordinal)
            .ToList();

        // Assert
        CoveredEndpoints.Should().HaveCount(29);
        registered.Should().BeEquivalentTo(CoveredEndpoints);
    }

    private static string NormalizePattern(RoutePattern routePattern) =>
        (routePattern.RawText ?? string.Empty).Trim('/');
}
