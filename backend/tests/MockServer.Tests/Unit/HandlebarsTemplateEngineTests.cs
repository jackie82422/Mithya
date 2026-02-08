using System.ComponentModel;
using FluentAssertions;
using MockServer.Core.Interfaces;
using MockServer.Infrastructure.MockEngine;
using Xunit;

namespace MockServer.Tests.Unit;

public class HandlebarsTemplateEngineTests
{
    private readonly ITemplateEngine _engine;

    public HandlebarsTemplateEngineTests()
    {
        _engine = new HandlebarsTemplateEngine();
    }

    private static TemplateContext CreateContext(
        string method = "GET",
        string path = "/api/test",
        string? body = null,
        Dictionary<string, string>? headers = null,
        Dictionary<string, string>? query = null,
        Dictionary<string, string>? pathParams = null)
    {
        return new TemplateContext
        {
            Request = new TemplateRequestData
            {
                Method = method,
                Path = path,
                Body = body,
                Headers = headers ?? new(),
                Query = query ?? new(),
                PathParams = pathParams ?? new()
            }
        };
    }

    [Fact]
    [DisplayName("渲染純文字模板應直接返回原文")]
    public void Render_PlainText_ShouldReturnAsIs()
    {
        // Arrange
        var template = "Hello World";
        var context = CreateContext();

        // Act
        var result = _engine.Render(template, context);

        // Assert
        result.Should().Be("Hello World");
    }

    [Fact]
    [DisplayName("渲染 request.method 應返回 HTTP 方法")]
    public void Render_RequestMethod_ShouldReturnMethod()
    {
        // Arrange
        var template = "{{Request.Method}}";
        var context = CreateContext(method: "POST");

        // Act
        var result = _engine.Render(template, context);

        // Assert
        result.Should().Be("POST");
    }

    [Fact]
    [DisplayName("渲染 request.path 應返回請求路徑")]
    public void Render_RequestPath_ShouldReturnPath()
    {
        // Arrange
        var template = "{{Request.Path}}";
        var context = CreateContext(path: "/api/users/123");

        // Act
        var result = _engine.Render(template, context);

        // Assert
        result.Should().Be("/api/users/123");
    }

    [Fact]
    [DisplayName("渲染 request.pathParams 應返回路徑參數值")]
    public void Render_PathParams_ShouldReturnParamValue()
    {
        // Arrange
        var template = "User ID: {{Request.PathParams.id}}";
        var context = CreateContext(pathParams: new Dictionary<string, string> { { "id", "42" } });

        // Act
        var result = _engine.Render(template, context);

        // Assert
        result.Should().Be("User ID: 42");
    }

    [Fact]
    [DisplayName("渲染 request.query 應返回查詢參數值")]
    public void Render_QueryParams_ShouldReturnQueryValue()
    {
        // Arrange
        var template = "Page: {{Request.Query.page}}";
        var context = CreateContext(query: new Dictionary<string, string> { { "page", "3" } });

        // Act
        var result = _engine.Render(template, context);

        // Assert
        result.Should().Be("Page: 3");
    }

    [Fact]
    [DisplayName("渲染 request.headers 應返回 Header 值")]
    public void Render_Headers_ShouldReturnHeaderValue()
    {
        // Arrange
        var template = "Accept: {{Request.Headers.Accept}}";
        var context = CreateContext(headers: new Dictionary<string, string> { { "Accept", "application/json" } });

        // Act
        var result = _engine.Render(template, context);

        // Assert
        result.Should().Be("Accept: application/json");
    }

    [Fact]
    [DisplayName("jsonPath helper 應從 JSON body 中擷取值")]
    public void Render_JsonPathHelper_ShouldExtractFromBody()
    {
        // Arrange
        var template = "Name: {{jsonPath Request.Body \"$.user.name\"}}";
        var context = CreateContext(body: "{\"user\":{\"name\":\"John\"}}");

        // Act
        var result = _engine.Render(template, context);

        // Assert
        result.Should().Be("Name: John");
    }

    [Fact]
    [DisplayName("now helper 應返回格式化日期")]
    public void Render_NowHelper_ShouldReturnFormattedDate()
    {
        // Arrange
        var template = "{{now \"yyyy-MM-dd\"}}";
        var context = CreateContext();

        // Act
        var result = _engine.Render(template, context);

        // Assert
        result.Should().Be(DateTime.UtcNow.ToString("yyyy-MM-dd"));
    }

    [Fact]
    [DisplayName("uuid helper 應返回有效的 GUID")]
    public void Render_UuidHelper_ShouldReturnValidGuid()
    {
        // Arrange
        var template = "{{uuid}}";
        var context = CreateContext();

        // Act
        var result = _engine.Render(template, context);

        // Assert
        Guid.TryParse(result, out _).Should().BeTrue();
    }

    [Fact]
    [DisplayName("randomInt helper 應返回範圍內的整數")]
    public void Render_RandomIntHelper_ShouldReturnIntInRange()
    {
        // Arrange
        var template = "{{randomInt 1 10}}";
        var context = CreateContext();

        // Act
        var result = _engine.Render(template, context);

        // Assert
        int.TryParse(result, out var value).Should().BeTrue();
        value.Should().BeInRange(1, 10);
    }

    [Fact]
    [DisplayName("math helper 加法應返回正確結果")]
    public void Render_MathAdd_ShouldReturnCorrectResult()
    {
        // Arrange
        var template = "{{math 5 \"+\" 3}}";
        var context = CreateContext();

        // Act
        var result = _engine.Render(template, context);

        // Assert
        result.Should().Be("8");
    }

    [Fact]
    [DisplayName("math helper 乘法應返回正確結果")]
    public void Render_MathMultiply_ShouldReturnCorrectResult()
    {
        // Arrange
        var template = "{{math 4 \"*\" 7}}";
        var context = CreateContext();

        // Act
        var result = _engine.Render(template, context);

        // Assert
        result.Should().Be("28");
    }

    [Fact]
    [DisplayName("eq helper 相等時應返回 true")]
    public void Render_EqHelper_Equal_ShouldReturnTrue()
    {
        // Arrange
        var template = "{{eq Request.Method \"GET\"}}";
        var context = CreateContext(method: "GET");

        // Act
        var result = _engine.Render(template, context);

        // Assert
        result.Should().Be("true");
    }

    [Fact]
    [DisplayName("eq helper 不相等時應返回 false")]
    public void Render_EqHelper_NotEqual_ShouldReturnFalse()
    {
        // Arrange
        var template = "{{eq Request.Method \"POST\"}}";
        var context = CreateContext(method: "GET");

        // Act
        var result = _engine.Render(template, context);

        // Assert
        result.Should().Be("false");
    }

    [Fact]
    [DisplayName("複合模板應正確渲染所有變數")]
    public void Render_ComplexTemplate_ShouldRenderAllVariables()
    {
        // Arrange
        var template = "{\"id\": \"{{Request.PathParams.id}}\", \"method\": \"{{Request.Method}}\", \"timestamp\": \"{{now \"yyyy-MM-dd\"}}\"}";
        var context = CreateContext(
            method: "GET",
            pathParams: new Dictionary<string, string> { { "id", "99" } }
        );

        // Act
        var result = _engine.Render(template, context);

        // Assert
        result.Should().Contain("\"id\": \"99\"");
        result.Should().Contain("\"method\": \"GET\"");
        result.Should().Contain(DateTime.UtcNow.ToString("yyyy-MM-dd"));
    }
}
