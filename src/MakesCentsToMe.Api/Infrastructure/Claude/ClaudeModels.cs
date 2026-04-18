namespace MakesCentsToMe.Api.Infrastructure.Claude;

public class ClaudeMessageRequest
{
    public int MaxTokens { get; set; } = 4096;
    public List<ClaudeRequestMessage> Messages { get; set; } = [];
    public string Model { get; set; } = "claude-sonnet-4-20250514";
    public string? System { get; set; }
}

public class ClaudeRequestMessage
{
    public string Content { get; set; } = string.Empty;
    public string Role { get; set; } = "user";
}

public class ClaudeMessageResponse
{
    public List<ClaudeContentBlock> Content { get; set; } = [];
}

public class ClaudeContentBlock
{
    public string? Text { get; set; }
    public string Type { get; set; } = string.Empty;
}

public class TransactionAnalysisResult
{
    public decimal Confidence { get; set; }
    public int Index { get; set; }
    public string NormalizedVendor { get; set; } = string.Empty;
    public string SuggestedCategory { get; set; } = string.Empty;
}
