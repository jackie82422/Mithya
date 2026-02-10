namespace Mithya.Core.Enums;

public enum MatchOperator
{
    Equals = 1,
    Contains = 2,
    Regex = 3,
    StartsWith = 4,
    EndsWith = 5,
    GreaterThan = 6,
    LessThan = 7,
    Exists = 8,
    NotEquals = 9,
    JsonSchema = 10,
    IsEmpty = 11,
    NotExists = 12
}

public enum LogicMode
{
    AND = 0,
    OR = 1
}
