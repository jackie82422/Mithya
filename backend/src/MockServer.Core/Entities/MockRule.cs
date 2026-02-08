using MockServer.Core.Enums;

namespace MockServer.Core.Entities;

public class MockRule
{
    public Guid Id { get; set; }
    public Guid EndpointId { get; set; }
    public string RuleName { get; set; } = string.Empty;
    public int Priority { get; set; }
    public string MatchConditions { get; set; } = string.Empty; // JSON array of MatchCondition

    public int ResponseStatusCode { get; set; }
    public string ResponseBody { get; set; } = string.Empty;
    public string? ResponseHeaders { get; set; }
    public int DelayMs { get; set; }
    public bool IsTemplate { get; set; }
    public bool IsResponseHeadersTemplate { get; set; }
    public FaultType FaultType { get; set; } = FaultType.None;
    public string? FaultConfig { get; set; }

    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public MockEndpoint Endpoint { get; set; } = null!;
}
