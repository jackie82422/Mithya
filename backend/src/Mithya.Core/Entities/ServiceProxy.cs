using Mithya.Core.Interfaces;

namespace Mithya.Core.Entities;

public class ServiceProxy : IProxyTarget
{
    public Guid Id { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public string TargetBaseUrl { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public bool IsRecording { get; set; }
    public bool ForwardHeaders { get; set; } = true;
    public string? AdditionalHeaders { get; set; }
    public int TimeoutMs { get; set; } = 10000;
    public string? StripPathPrefix { get; set; }
    public bool FallbackEnabled { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
