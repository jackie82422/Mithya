using System.ComponentModel;
using FluentAssertions;
using Mithya.Core.Entities;
using Mithya.Core.Enums;
using Mithya.Infrastructure.ProtocolHandlers;
using Newtonsoft.Json;
using Xunit;

namespace Mithya.Tests.Unit;

public class SoapProtocolHandlerTests
{
    private readonly SoapProtocolHandler _handler;

    public SoapProtocolHandlerTests()
    {
        _handler = new SoapProtocolHandler();
    }

    [Fact]
    [DisplayName("取得 SOAP 協議 Schema")]
    public void GetSchema_ShouldReturnSoapSchema()
    {
        // Act
        var schema = _handler.GetSchema();

        // Assert
        schema.Should().NotBeNull();
        schema.Protocol.Should().Be(ProtocolType.SOAP);
        schema.Name.Should().Be("SOAP/XML");
        schema.SupportedSources.Should().Contain(FieldSourceType.Body);
        schema.SupportedSources.Should().Contain(FieldSourceType.Header);
    }

    [Fact]
    [DisplayName("驗證有效的 SOAP Endpoint 應該返回成功")]
    public void ValidateEndpoint_ValidSoapEndpoint_ShouldReturnSuccess()
    {
        // Arrange
        var endpoint = new MockEndpoint
        {
            Path = "/soap/service",
            HttpMethod = "POST",
            Protocol = ProtocolType.SOAP
        };

        // Act
        var result = _handler.ValidateEndpoint(endpoint);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    [DisplayName("驗證非 POST 方法的 SOAP Endpoint 應該返回錯誤")]
    public void ValidateEndpoint_NonPostMethod_ShouldReturnError()
    {
        // Arrange
        var endpoint = new MockEndpoint
        {
            Path = "/soap/service",
            HttpMethod = "GET",
            Protocol = ProtocolType.SOAP
        };

        // Act
        var result = _handler.ValidateEndpoint(endpoint);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain("SOAP endpoints must use POST method");
    }

    [Fact]
    [DisplayName("驗證有效的 XPath Rule 應該返回成功")]
    public void ValidateRule_ValidXPath_ShouldReturnSuccess()
    {
        // Arrange
        var endpoint = new MockEndpoint
        {
            Id = Guid.NewGuid(),
            Path = "/soap/service",
            HttpMethod = "POST",
            Protocol = ProtocolType.SOAP
        };

        var conditions = new List<MatchCondition>
        {
            new MatchCondition
            {
                SourceType = FieldSourceType.Body,
                FieldPath = "//*[local-name()='userId']",
                Operator = MatchOperator.Equals,
                Value = "123"
            }
        };

        var rule = new MockRule
        {
            EndpointId = endpoint.Id,
            RuleName = "SOAP Rule",
            MatchConditions = JsonConvert.SerializeObject(conditions),
            ResponseStatusCode = 200,
            ResponseBody = "<response/>"
        };

        // Act
        var result = _handler.ValidateRule(rule, endpoint);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }
}
