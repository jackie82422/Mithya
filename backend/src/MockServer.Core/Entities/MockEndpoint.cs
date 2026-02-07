using MockServer.Core.Enums;

namespace MockServer.Core.Entities;

public class MockEndpoint
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public ProtocolType Protocol { get; set; }
    public string Path { get; set; } = string.Empty;
    public string HttpMethod { get; set; } = string.Empty;

    public string? DefaultResponse { get; set; }
    public int? DefaultStatusCode { get; set; }

    public string? ProtocolSettings { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<MockRule> Rules { get; set; } = new List<MockRule>();
}
