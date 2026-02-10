using System.ComponentModel;
using FluentAssertions;
using Mithya.Core.Enums;
using Mithya.Infrastructure.MockEngine;
using Xunit;

namespace Mithya.Tests.Unit;

public class OperatorEvaluatorTests
{
    [Fact]
    [DisplayName("NotEquals 當值不同時應返回 true")]
    public void NotEquals_DifferentValues_ShouldReturnTrue()
    {
        OperatorEvaluator.Evaluate(MatchOperator.NotEquals, "hello", "world").Should().BeTrue();
    }

    [Fact]
    [DisplayName("NotEquals 當值相同時應返回 false")]
    public void NotEquals_SameValues_ShouldReturnFalse()
    {
        OperatorEvaluator.Evaluate(MatchOperator.NotEquals, "hello", "hello").Should().BeFalse();
    }

    [Fact]
    [DisplayName("IsEmpty 當值為空字串時應返回 true")]
    public void IsEmpty_EmptyString_ShouldReturnTrue()
    {
        OperatorEvaluator.Evaluate(MatchOperator.IsEmpty, "", "").Should().BeTrue();
    }

    [Fact]
    [DisplayName("IsEmpty 當值為 null 時應返回 true")]
    public void IsEmpty_Null_ShouldReturnTrue()
    {
        OperatorEvaluator.Evaluate(MatchOperator.IsEmpty, null, "").Should().BeTrue();
    }

    [Fact]
    [DisplayName("IsEmpty 當值非空時應返回 false")]
    public void IsEmpty_NonEmpty_ShouldReturnFalse()
    {
        OperatorEvaluator.Evaluate(MatchOperator.IsEmpty, "hello", "").Should().BeFalse();
    }

    [Fact]
    [DisplayName("NotExists 當值為 null 時應返回 true")]
    public void NotExists_Null_ShouldReturnTrue()
    {
        OperatorEvaluator.Evaluate(MatchOperator.NotExists, null, "").Should().BeTrue();
    }

    [Fact]
    [DisplayName("NotExists 當值存在時應返回 false")]
    public void NotExists_HasValue_ShouldReturnFalse()
    {
        OperatorEvaluator.Evaluate(MatchOperator.NotExists, "value", "").Should().BeFalse();
    }

    [Fact]
    [DisplayName("JsonSchema 驗證有效 JSON 應返回 true")]
    public void JsonSchema_ValidJson_ShouldReturnTrue()
    {
        var schema = """{"type":"object","properties":{"name":{"type":"string"}},"required":["name"]}""";
        var json = """{"name":"John"}""";

        OperatorEvaluator.Evaluate(MatchOperator.JsonSchema, json, schema).Should().BeTrue();
    }

    [Fact]
    [DisplayName("JsonSchema 驗證無效 JSON 應返回 false")]
    public void JsonSchema_InvalidJson_ShouldReturnFalse()
    {
        var schema = """{"type":"object","properties":{"name":{"type":"string"}},"required":["name"]}""";
        var json = """{"age":30}""";

        OperatorEvaluator.Evaluate(MatchOperator.JsonSchema, json, schema).Should().BeFalse();
    }

    [Fact]
    [DisplayName("JsonSchema 當 JSON 為 null 時應返回 false")]
    public void JsonSchema_NullJson_ShouldReturnFalse()
    {
        var schema = """{"type":"object"}""";
        OperatorEvaluator.Evaluate(MatchOperator.JsonSchema, null, schema).Should().BeFalse();
    }
}
