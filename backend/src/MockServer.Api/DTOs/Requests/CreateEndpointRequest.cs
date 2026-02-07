using MockServer.Core.Enums;

namespace MockServer.Api.DTOs.Requests;

public class CreateEndpointRequest
{
    public string Name { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public ProtocolType Protocol { get; set; }
    public string Path { get; set; } = string.Empty;
    public string HttpMethod { get; set; } = string.Empty;
    public string? ProtocolSettings { get; set; }
}
