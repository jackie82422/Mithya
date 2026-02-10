using System.ComponentModel;
using FluentAssertions;
using Mithya.Infrastructure.MockEngine;
using Xunit;

namespace Mithya.Tests.Unit;

public class PathMatcherTests
{
    // === IsMatch Tests ===

    [Fact]
    [DisplayName("完全匹配路徑應返回 true")]
    public void IsMatch_ExactMatch_ShouldReturnTrue()
    {
        PathMatcher.IsMatch("/api/users", "/api/users").Should().BeTrue();
    }

    [Fact]
    [DisplayName("完全匹配應忽略大小寫")]
    public void IsMatch_CaseInsensitive_ShouldReturnTrue()
    {
        PathMatcher.IsMatch("/api/Users", "/API/USERS").Should().BeTrue();
    }

    [Fact]
    [DisplayName("不匹配路徑應返回 false")]
    public void IsMatch_NoMatch_ShouldReturnFalse()
    {
        PathMatcher.IsMatch("/api/users", "/api/orders").Should().BeFalse();
    }

    [Fact]
    [DisplayName("帶路徑參數的路徑應匹配")]
    public void IsMatch_WithPathParam_ShouldMatch()
    {
        PathMatcher.IsMatch("/api/users/{id}", "/api/users/123").Should().BeTrue();
    }

    [Fact]
    [DisplayName("多個路徑參數應匹配")]
    public void IsMatch_MultiplePathParams_ShouldMatch()
    {
        PathMatcher.IsMatch("/api/users/{userId}/orders/{orderId}", "/api/users/1/orders/42")
            .Should().BeTrue();
    }

    [Fact]
    [DisplayName("路徑參數不匹配多段路徑")]
    public void IsMatch_PathParamShouldNotMatchSlash()
    {
        PathMatcher.IsMatch("/api/users/{id}", "/api/users/1/extra").Should().BeFalse();
    }

    [Fact]
    [DisplayName("段落數量不同應不匹配")]
    public void IsMatch_DifferentSegmentCount_ShouldNotMatch()
    {
        PathMatcher.IsMatch("/api/users/{id}/orders", "/api/users/1").Should().BeFalse();
    }

    [Fact]
    [DisplayName("靜態路徑中間不同應不匹配")]
    public void IsMatch_StaticSegmentMismatch_ShouldNotMatch()
    {
        PathMatcher.IsMatch("/api/users/{id}", "/api/orders/123").Should().BeFalse();
    }

    // === ExtractPathParams Tests ===

    [Fact]
    [DisplayName("無路徑參數應返回空字典")]
    public void ExtractPathParams_NoParams_ShouldReturnEmpty()
    {
        var result = PathMatcher.ExtractPathParams("/api/users", "/api/users");
        result.Should().BeEmpty();
    }

    [Fact]
    [DisplayName("應正確抽取單一路徑參數")]
    public void ExtractPathParams_SingleParam_ShouldExtract()
    {
        var result = PathMatcher.ExtractPathParams("/api/users/{id}", "/api/users/123");
        result.Should().ContainKey("id");
        result["id"].Should().Be("123");
    }

    [Fact]
    [DisplayName("應正確抽取多個路徑參數")]
    public void ExtractPathParams_MultipleParams_ShouldExtract()
    {
        var result = PathMatcher.ExtractPathParams(
            "/api/users/{userId}/orders/{orderId}",
            "/api/users/abc/orders/xyz");

        result.Should().HaveCount(2);
        result["userId"].Should().Be("abc");
        result["orderId"].Should().Be("xyz");
    }

    [Fact]
    [DisplayName("路徑參數查詢應忽略大小寫")]
    public void ExtractPathParams_ShouldBeCaseInsensitive()
    {
        var result = PathMatcher.ExtractPathParams("/api/users/{id}", "/API/USERS/456");
        result["id"].Should().Be("456");
    }
}
