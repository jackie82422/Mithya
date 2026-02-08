namespace MockServer.Core.Entities;

public class Scenario
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string InitialState { get; set; } = string.Empty;
    public string CurrentState { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<ScenarioStep> Steps { get; set; } = new List<ScenarioStep>();
}
