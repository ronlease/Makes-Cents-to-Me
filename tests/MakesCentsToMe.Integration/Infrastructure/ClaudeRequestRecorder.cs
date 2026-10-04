namespace MakesCentsToMe.Integration.Infrastructure;

/// <summary>
/// One request the application sent to the stubbed Claude endpoint.
/// </summary>
public sealed record RecordedClaudeRequest(string Body, IReadOnlyList<string> Descriptions, Uri? RequestUri);

/// <summary>
/// Thread-safe log of the requests the application sent to the stubbed Claude endpoint.
/// </summary>
public sealed class ClaudeRequestRecorder
{
    private readonly object _lock = new();
    private readonly List<RecordedClaudeRequest> _requests = [];

    public int Count
    {
        get
        {
            lock (_lock)
            {
                return _requests.Count;
            }
        }
    }

    /// <summary>Transaction descriptions across every recorded request, in order.</summary>
    public IReadOnlyList<string> Descriptions => Requests.SelectMany(request => request.Descriptions).ToList();

    public IReadOnlyList<RecordedClaudeRequest> Requests
    {
        get
        {
            lock (_lock)
            {
                return _requests.ToList();
            }
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            _requests.Clear();
        }
    }

    public void Record(RecordedClaudeRequest request)
    {
        lock (_lock)
        {
            _requests.Add(request);
        }
    }
}
