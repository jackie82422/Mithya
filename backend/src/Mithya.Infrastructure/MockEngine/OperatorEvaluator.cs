using System.Text.RegularExpressions;
using Mithya.Core.Enums;
using NJsonSchema;

namespace Mithya.Infrastructure.MockEngine;

public static class OperatorEvaluator
{
    public static bool Evaluate(MatchOperator op, string? actual, string expected)
    {
        return op switch
        {
            MatchOperator.Equals => string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase),
            MatchOperator.Contains => actual?.Contains(expected, StringComparison.OrdinalIgnoreCase) == true,
            MatchOperator.Regex => actual != null && Regex.IsMatch(actual, expected),
            MatchOperator.StartsWith => actual?.StartsWith(expected, StringComparison.OrdinalIgnoreCase) == true,
            MatchOperator.EndsWith => actual?.EndsWith(expected, StringComparison.OrdinalIgnoreCase) == true,
            MatchOperator.GreaterThan => decimal.TryParse(actual, out var a) && decimal.TryParse(expected, out var ge) && a > ge,
            MatchOperator.LessThan => decimal.TryParse(actual, out var b) && decimal.TryParse(expected, out var le) && b < le,
            MatchOperator.Exists => actual != null,
            MatchOperator.NotEquals => !string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase),
            MatchOperator.JsonSchema => ValidateJsonSchema(actual, expected),
            MatchOperator.IsEmpty => string.IsNullOrEmpty(actual),
            MatchOperator.NotExists => actual == null,
            _ => false
        };
    }

    private static bool ValidateJsonSchema(string? json, string schema)
    {
        if (json == null) return false;
        try
        {
            var jsonSchema = JsonSchema.FromJsonAsync(schema).Result;
            var errors = jsonSchema.Validate(json);
            return errors.Count == 0;
        }
        catch
        {
            return false;
        }
    }
}
