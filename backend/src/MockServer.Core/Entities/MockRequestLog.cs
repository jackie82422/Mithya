namespace MockServer.Core.Entities;

public class MockRequestLog
{
    public Guid Id { get; set; }
    public Guid? EndpointId { get; set; }
    public Guid? RuleId { get; set; }
    public DateTime Timestamp { get; set; }

    public string Method { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string? QueryString { get; set; }
    public string? Headers { get; set; }
    public string? Body { get; set; }

    public int ResponseStatusCode { get; set; }
    public string? ResponseBody { get; set; }
    public int ResponseTimeMs { get; set; }

    public bool IsMatched { get; set; }
    public int? FaultTypeApplied { get; set; }
}
