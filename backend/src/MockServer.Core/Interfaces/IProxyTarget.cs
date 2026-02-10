namespace MockServer.Core.Interfaces;

public interface IProxyTarget
{
    string TargetBaseUrl { get; }
    bool ForwardHeaders { get; }
    string? AdditionalHeaders { get; }
    int TimeoutMs { get; }
    string? StripPathPrefix { get; }
    bool IsRecording { get; }
}
