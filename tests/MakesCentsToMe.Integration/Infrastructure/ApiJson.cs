using FluentAssertions;
using MakesCentsToMe.Api.Common;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MakesCentsToMe.Integration.Infrastructure;

/// <summary>
/// JSON helpers that mirror the API's serializer configuration (camelCase, string enums).
/// </summary>
public static class ApiJson
{
    public static JsonSerializerOptions Options { get; } = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    /// <summary>Asserts the top-level properties are exactly data, errors, success (camelCase).</summary>
    public static void AssertApiResponseEnvelope(JsonDocument document)
    {
        document.RootElement.ValueKind.Should().Be(JsonValueKind.Object);
        var propertyNames = document.RootElement.EnumerateObject().Select(property => property.Name);
        propertyNames.Should().BeEquivalentTo(["data", "errors", "success"]);
    }

    public static async Task<ApiResponse<T>> ReadApiResponseAsync<T>(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponse<T>>(body, Options);
        return result ?? throw new InvalidOperationException($"Response body was not an ApiResponse: '{body}'.");
    }

    public static async Task<JsonDocument> ReadJsonDocumentAsync(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync());
}
