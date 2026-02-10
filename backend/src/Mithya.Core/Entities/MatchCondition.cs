using Mithya.Core.Enums;

namespace Mithya.Core.Entities;

public class MatchCondition
{
    public FieldSourceType SourceType { get; set; }
    public string FieldPath { get; set; } = string.Empty;
    public MatchOperator Operator { get; set; }
    public string Value { get; set; } = string.Empty;
}
