using Mithya.Core.Enums;

namespace Mithya.Core.ValueObjects;

public class CachedEndpoint
{
    public Guid Id { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string HttpMethod { get; set; } = string.Empty;
    public ProtocolType Protocol { get; set; }
    public bool IsActive { get; set; }
    public string? DefaultResponse { get; set; }
    public int? DefaultStatusCode { get; set; }
    public List<CachedRule> Rules { get; set; } = new();
}
