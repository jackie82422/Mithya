namespace MockServer.Core.Entities;

public class ProxyConfig
{
    public Guid Id { get; set; }
    public Guid? EndpointId { get; set; }
    public string TargetBaseUrl { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public bool IsRecording { get; set; }
    public bool ForwardHeaders { get; set; } = true;
    public string? AdditionalHeaders { get; set; }
    public int TimeoutMs { get; set; } = 10000;
    public string? StripPathPrefix { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public MockEndpoint? Endpoint { get; set; }
}
