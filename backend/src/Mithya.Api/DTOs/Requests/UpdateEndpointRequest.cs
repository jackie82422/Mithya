namespace Mithya.Api.DTOs.Requests;

public class UpdateEndpointRequest
{
    public string Name { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string HttpMethod { get; set; } = string.Empty;
    public string? ProtocolSettings { get; set; }
}
