namespace Mithya.Core.Entities;

public class EndpointGroupMapping
{
    public Guid GroupId { get; set; }
    public Guid EndpointId { get; set; }

    public EndpointGroup Group { get; set; } = null!;
    public MockEndpoint Endpoint { get; set; } = null!;
}
