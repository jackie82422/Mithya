using MockServer.Core.Entities;

namespace MockServer.Core.ValueObjects;

public class CachedRule
{
    public Guid Id { get; set; }
    public Guid EndpointId { get; set; }
    public string RuleName { get; set; } = string.Empty;
    public int Priority { get; set; }
    public List<MatchCondition> Conditions { get; set; } = new();
    public int ResponseStatusCode { get; set; }
    public string ResponseBody { get; set; } = string.Empty;
    public Dictionary<string, string>? ResponseHeaders { get; set; }
    public int DelayMs { get; set; }
    public bool IsTemplate { get; set; }
    public bool IsResponseHeadersTemplate { get; set; }
}
