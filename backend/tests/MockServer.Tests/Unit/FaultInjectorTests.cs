using System.ComponentModel;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using MockServer.Core.Enums;
using MockServer.Core.ValueObjects;
using MockServer.Infrastructure.MockEngine;
using Newtonsoft.Json;
using Xunit;

namespace MockServer.Tests.Unit;

public class FaultInjectorTests
{
    private readonly FaultInjector _injector;

    public FaultInjectorTests()
    {
        _injector = new FaultInjector();
    }

    private static CachedRule CreateRule(FaultType faultType, object? faultConfig = null)
    {
        return new CachedRule
        {
            Id = Guid.NewGuid(),
            EndpointId = Guid.NewGuid(),
            RuleName = "Test Rule",
            Priority = 1,
            ResponseStatusCode = 200,
            ResponseBody = "{}",
            FaultType = faultType,
            FaultConfig = faultConfig != null ? JsonConvert.SerializeObject(faultConfig) : null
        };
    }

    private static HttpContext CreateHttpContext()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        return context;
    }

    [Fact]
    [DisplayName("FaultType.None 不應注入故障")]
    public async Task ApplyFault_None_ShouldReturnFalse()
    {
        // Arrange
        var rule = CreateRule(FaultType.None);
        var httpContext = CreateHttpContext();

        // Act
        var result = await _injector.ApplyFaultAsync(httpContext, rule);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    [DisplayName("FaultType.FixedDelay 不應由 FaultInjector 處理")]
    public async Task ApplyFault_FixedDelay_ShouldReturnFalse()
    {
        // Arrange
        var rule = CreateRule(FaultType.FixedDelay);
        var httpContext = CreateHttpContext();

        // Act
        var result = await _injector.ApplyFaultAsync(httpContext, rule);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    [DisplayName("FaultType.RandomDelay 應延遲後返回 false（繼續正常回應）")]
    public async Task ApplyFault_RandomDelay_ShouldDelayAndReturnFalse()
    {
        // Arrange
        var rule = CreateRule(FaultType.RandomDelay, new { minDelayMs = 1, maxDelayMs = 10 });
        var httpContext = CreateHttpContext();

        // Act
        var result = await _injector.ApplyFaultAsync(httpContext, rule);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    [DisplayName("FaultType.EmptyResponse 應設定狀態碼並返回 true")]
    public async Task ApplyFault_EmptyResponse_ShouldSetStatusCodeAndReturnTrue()
    {
        // Arrange
        var rule = CreateRule(FaultType.EmptyResponse, new { statusCode = 503 });
        var httpContext = CreateHttpContext();

        // Act
        var result = await _injector.ApplyFaultAsync(httpContext, rule);

        // Assert
        result.Should().BeTrue();
        httpContext.Response.StatusCode.Should().Be(503);
    }

    [Fact]
    [DisplayName("FaultType.EmptyResponse 無配置時應使用預設 503")]
    public async Task ApplyFault_EmptyResponse_NoConfig_ShouldDefault503()
    {
        // Arrange
        var rule = CreateRule(FaultType.EmptyResponse);
        var httpContext = CreateHttpContext();

        // Act
        var result = await _injector.ApplyFaultAsync(httpContext, rule);

        // Assert
        result.Should().BeTrue();
        httpContext.Response.StatusCode.Should().Be(503);
    }

    [Fact]
    [DisplayName("FaultType.MalformedResponse 應寫入隨機位元組並返回 true")]
    public async Task ApplyFault_MalformedResponse_ShouldWriteRandomBytesAndReturnTrue()
    {
        // Arrange
        var rule = CreateRule(FaultType.MalformedResponse, new { byteCount = 64 });
        var httpContext = CreateHttpContext();

        // Act
        var result = await _injector.ApplyFaultAsync(httpContext, rule);

        // Assert
        result.Should().BeTrue();
        httpContext.Response.ContentType.Should().Be("application/octet-stream");
        httpContext.Response.Body.Length.Should().Be(64);
    }

    [Fact]
    [DisplayName("FaultType.MalformedResponse 無配置時應使用預設 256 位元組")]
    public async Task ApplyFault_MalformedResponse_NoConfig_ShouldDefault256Bytes()
    {
        // Arrange
        var rule = CreateRule(FaultType.MalformedResponse);
        var httpContext = CreateHttpContext();

        // Act
        var result = await _injector.ApplyFaultAsync(httpContext, rule);

        // Assert
        result.Should().BeTrue();
        httpContext.Response.Body.Length.Should().Be(256);
    }
}
