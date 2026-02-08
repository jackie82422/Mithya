namespace MockServer.Core.Entities;

public class ScenarioStep
{
    public Guid Id { get; set; }
    public Guid ScenarioId { get; set; }
    public string StateName { get; set; } = string.Empty;
    public Guid EndpointId { get; set; }
    public string? MatchConditions { get; set; }
    public int ResponseStatusCode { get; set; } = 200;
    public string ResponseBody { get; set; } = string.Empty;
    public string? ResponseHeaders { get; set; }
    public bool IsTemplate { get; set; }
    public int DelayMs { get; set; }
    public string? NextState { get; set; }
    public int Priority { get; set; } = 100;

    public Scenario Scenario { get; set; } = null!;
    public MockEndpoint Endpoint { get; set; } = null!;
}
