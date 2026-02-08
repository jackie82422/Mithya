using MockServer.Core.Enums;
using MockServer.Core.ValueObjects;

namespace MockServer.Core.Interfaces;

public class MockRequestContext
{
    public string Method { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string? QueryString { get; set; }
    public Dictionary<string, string> QueryParams { get; set; } = new();
    public Dictionary<string, string> Headers { get; set; } = new();
    public string? Body { get; set; }
}

public class MatchResult
{
    public CachedEndpoint Endpoint { get; set; } = null!;
    public CachedRule? Rule { get; set; }
    public bool IsDefaultResponse { get; set; }
}

public interface IMatchEngine
{
    Task<MatchResult?> FindMatchAsync(MockRequestContext context);
}
