using System.ComponentModel;
using FluentAssertions;
using MockServer.Core.Entities;
using MockServer.Core.Enums;
using MockServer.Infrastructure.ProtocolHandlers;
using Newtonsoft.Json;
using Xunit;

namespace MockServer.Tests.Unit;

public class RestProtocolHandlerTests
{
    private readonly RestProtocolHandler _handler;

    public RestProtocolHandlerTests()
    {
        _handler = new RestProtocolHandler();
    }

    [Fact]
    [DisplayName("取得 REST 協議 Schema")]
    public void GetSchema_ShouldReturnRestSchema()
    {
        // Act
        var schema = _handler.GetSchema();

        // Assert
        schema.Should().NotBeNull();
        schema.Protocol.Should().Be(ProtocolType.REST);
        schema.Name.Should().Be("REST/JSON");
        schema.SupportedSources.Should().Contain(FieldSourceType.Body);
        schema.SupportedSources.Should().Contain(FieldSourceType.Header);
        schema.SupportedSources.Should().Contain(FieldSourceType.Query);
    }

    [Fact]
    [DisplayName("驗證有效的 REST Endpoint 應該返回成功")]
    public void ValidateEndpoint_ValidRestEndpoint_ShouldReturnSuccess()
    {
        // Arrange
        var endpoint = new MockEndpoint
        {
            Path = "/api/test",
            HttpMethod = "POST",
            Protocol = ProtocolType.REST
        };

        // Act
        var result = _handler.ValidateEndpoint(endpoint);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    [DisplayName("驗證缺少 Path 的 Endpoint 應該返回錯誤")]
    public void ValidateEndpoint_MissingPath_ShouldReturnError()
    {
        // Arrange
        var endpoint = new MockEndpoint
        {
            Path = "",
            HttpMethod = "POST",
            Protocol = ProtocolType.REST
        };

        // Act
        var result = _handler.ValidateEndpoint(endpoint);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain("Path is required");
    }

    [Fact]
    [DisplayName("驗證無效的 HttpMethod 應該返回錯誤")]
    public void ValidateEndpoint_InvalidHttpMethod_ShouldReturnError()
    {
        // Arrange
        var endpoint = new MockEndpoint
        {
            Path = "/api/test",
            HttpMethod = "INVALID",
            Protocol = ProtocolType.REST
        };

        // Act
        var result = _handler.ValidateEndpoint(endpoint);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("HttpMethod must be one of"));
    }

    [Fact]
    [DisplayName("驗證有效的 Rule 應該返回成功")]
    public void ValidateRule_ValidRule_ShouldReturnSuccess()
    {
        // Arrange
        var endpoint = new MockEndpoint
        {
            Id = Guid.NewGuid(),
            Path = "/api/test",
            HttpMethod = "POST",
            Protocol = ProtocolType.REST
        };

        var conditions = new List<MatchCondition>
        {
            new MatchCondition
            {
                SourceType = FieldSourceType.Body,
                FieldPath = "$.userId",
                Operator = MatchOperator.Equals,
                Value = "123"
            }
        };

        var rule = new MockRule
        {
            EndpointId = endpoint.Id,
            RuleName = "Test Rule",
            MatchConditions = JsonConvert.SerializeObject(conditions),
            ResponseStatusCode = 200,
            ResponseBody = "{}"
        };

        // Act
        var result = _handler.ValidateRule(rule, endpoint);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    [DisplayName("驗證無效的 JsonPath 應該返回錯誤")]
    public void ValidateRule_InvalidJsonPath_ShouldReturnError()
    {
        // Arrange
        var endpoint = new MockEndpoint
        {
            Id = Guid.NewGuid(),
            Path = "/api/test",
            HttpMethod = "POST",
            Protocol = ProtocolType.REST
        };

        var conditions = new List<MatchCondition>
        {
            new MatchCondition
            {
                SourceType = FieldSourceType.Body,
                FieldPath = "userId", // Should start with $.
                Operator = MatchOperator.Equals,
                Value = "123"
            }
        };

        var rule = new MockRule
        {
            EndpointId = endpoint.Id,
            RuleName = "Test Rule",
            MatchConditions = JsonConvert.SerializeObject(conditions),
            ResponseStatusCode = 200,
            ResponseBody = "{}"
        };

        // Act
        var result = _handler.ValidateRule(rule, endpoint);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("JsonPath"));
    }

    [Fact]
    [DisplayName("驗證無效的 StatusCode 應該返回錯誤")]
    public void ValidateRule_InvalidStatusCode_ShouldReturnError()
    {
        // Arrange
        var endpoint = new MockEndpoint
        {
            Id = Guid.NewGuid(),
            Path = "/api/test",
            HttpMethod = "POST",
            Protocol = ProtocolType.REST
        };

        var conditions = new List<MatchCondition>
        {
            new MatchCondition
            {
                SourceType = FieldSourceType.Body,
                FieldPath = "$.userId",
                Operator = MatchOperator.Equals,
                Value = "123"
            }
        };

        var rule = new MockRule
        {
            EndpointId = endpoint.Id,
            RuleName = "Test Rule",
            MatchConditions = JsonConvert.SerializeObject(conditions),
            ResponseStatusCode = 999, // Invalid
            ResponseBody = "{}"
        };

        // Act
        var result = _handler.ValidateRule(rule, endpoint);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("ResponseStatusCode"));
    }
}
