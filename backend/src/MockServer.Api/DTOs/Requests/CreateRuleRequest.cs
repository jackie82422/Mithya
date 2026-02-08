using MockServer.Core.Entities;
using MockServer.Core.Enums;

namespace MockServer.Api.DTOs.Requests;

public class CreateRuleRequest
{
    public string RuleName { get; set; } = string.Empty;
    public int Priority { get; set; } = 100;
    public List<MatchCondition> Conditions { get; set; } = new();
    public int StatusCode { get; set; } = 200;
    public string ResponseBody { get; set; } = string.Empty;
    public Dictionary<string, string>? ResponseHeaders { get; set; }
    public int DelayMs { get; set; } = 0;
    public bool IsTemplate { get; set; } = false;
    public bool IsResponseHeadersTemplate { get; set; } = false;
    public FaultType FaultType { get; set; } = FaultType.None;
    public string? FaultConfig { get; set; }
    public LogicMode LogicMode { get; set; } = LogicMode.AND;
}
