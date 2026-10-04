using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace MakesCentsToMe.Integration.Infrastructure;

/// <summary>
/// Stands in for api.anthropic.com. Always answers 200 (the service retries non-success responses with
/// multi-second delays) with a deterministic analysis: vendor is the description up to the first '#' or '*',
/// category is "Dining", confidence is 0.9.
/// </summary>
public sealed partial class StubClaudeMessageHandler(ClaudeRequestRecorder recorder) : HttpMessageHandler
{
    public const string SuggestedCategory = "Dining";

    public static string DeriveVendor(string description)
    {
        var cutIndex = description.IndexOfAny(['#', '*']);
        var vendor = cutIndex >= 0 ? description[..cutIndex] : description;
        return vendor.Trim();
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);

        string content;
        using (var document = JsonDocument.Parse(body))
        {
            content = document.RootElement.GetProperty("messages")[0].GetProperty("content").GetString() ?? string.Empty;
        }

        var indexedDescriptions = TransactionLinePattern().Matches(content)
            .Select(match => (Description: match.Groups[2].Value, Index: int.Parse(match.Groups[1].Value)))
            .ToList();

        recorder.Record(new RecordedClaudeRequest(
            body,
            indexedDescriptions.Select(item => item.Description).ToList(),
            request.RequestUri));

        var results = indexedDescriptions.Select(item => new
        {
            confidence = 0.9m,
            index = item.Index,
            normalizedVendor = DeriveVendor(item.Description),
            suggestedCategory = SuggestedCategory,
        });

        var responseBody = JsonSerializer.Serialize(new
        {
            content = new[] { new { text = JsonSerializer.Serialize(results), type = "text" } },
        });

        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseBody, Encoding.UTF8, "application/json"),
        };
    }

    [GeneratedRegex("""^\s*(\d+): "(.*)"\r?$""", RegexOptions.Multiline)]
    private static partial Regex TransactionLinePattern();
}
