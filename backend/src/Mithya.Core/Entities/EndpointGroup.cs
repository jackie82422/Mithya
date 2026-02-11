namespace Mithya.Core.Entities;

public class EndpointGroup
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Color { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<EndpointGroupMapping> Mappings { get; set; } = new List<EndpointGroupMapping>();
}
