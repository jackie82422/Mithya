using MockServer.Core.Enums;

namespace MockServer.Core.ValueObjects;

public class ProtocolSchema
{
    public ProtocolType Protocol { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<FieldSourceType> SupportedSources { get; set; } = new();
    public List<MatchOperator> SupportedOperators { get; set; } = new();
    public Dictionary<string, string> ExampleFieldPaths { get; set; } = new();
}
