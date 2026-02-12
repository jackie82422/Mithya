using System.ComponentModel;
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mithya.Core.Entities;
using Mithya.Core.Enums;
using Mithya.Infrastructure.Data;
using Xunit;

namespace Mithya.Tests.Integration;

public class FaultInjectionApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public FaultInjectionApiTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<MithyaDbContext>));
                if (descriptor != null)
                    services.Remove(descriptor);

                services.AddDbContext<MithyaDbContext>(options =>
                    options.UseInMemoryDatabase("TestDb_Fault"));

                var sp = services.BuildServiceProvider();
                using var scope = sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<MithyaDbContext>();
                db.Database.EnsureCreated();
            });
        });
    }

    [Fact]
    [DisplayName("建立 Rule 時應支援 FaultType 欄位")]
    public async Task CreateRule_WithFaultType_ShouldPersist()
    {
        // Arrange
        var client = _factory.CreateClient();

        var endpointResponse = await client.PostAsJsonAsync("/admin/api/endpoints", new
        {
            name = "Fault Persist Test",
            serviceName = "Test",
            protocol = 1,
            path = "/api/fault-persist",
            httpMethod = "GET"
        });
        endpointResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var endpoint = await endpointResponse.Content.ReadFromJsonAsync<MockEndpoint>();

        // Act
        var response = await client.PostAsJsonAsync($"/admin/api/endpoints/{endpoint!.Id}/rules", new
        {
            ruleName = "Fault Rule",
            priority = 1,
            conditions = Array.Empty<object>(),
            statusCode = 200,
            responseBody = "{}",
            faultType = (int)FaultType.EmptyResponse,
            faultConfig = "{\"statusCode\": 503}"
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var rule = await response.Content.ReadFromJsonAsync<MockRule>();
        rule.Should().NotBeNull();
        rule!.FaultType.Should().Be(FaultType.EmptyResponse);
        rule.FaultConfig.Should().Contain("503");
    }

    [Fact]
    [DisplayName("EmptyResponse 故障應返回空 Body 和指定狀態碼")]
    public async Task MockRequest_EmptyResponseFault_ShouldReturnEmptyBody()
    {
        // Arrange
        var client = _factory.CreateClient();

        var endpointResponse = await client.PostAsJsonAsync("/admin/api/endpoints", new
        {
            name = "Empty Response Test",
            serviceName = "Test",
            protocol = 1,
            path = "/api/fault-empty",
            httpMethod = "GET"
        });
        endpointResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var endpoint = await endpointResponse.Content.ReadFromJsonAsync<MockEndpoint>();

        var ruleResponse = await client.PostAsJsonAsync($"/admin/api/endpoints/{endpoint!.Id}/rules", new
        {
            ruleName = "Empty Response Fault",
            priority = 1,
            conditions = Array.Empty<object>(),
            statusCode = 200,
            responseBody = "{\"should\":\"not appear\"}",
            faultType = (int)FaultType.EmptyResponse,
            faultConfig = "{\"statusCode\": 503}"
        });
        ruleResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Act
        var mockResponse = await client.GetAsync("/api/fault-empty");

        // Assert
        mockResponse.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        var body = await mockResponse.Content.ReadAsStringAsync();
        body.Should().BeEmpty();
    }

    [Fact]
    [DisplayName("MalformedResponse 故障應返回隨機位元組")]
    public async Task MockRequest_MalformedFault_ShouldReturnRandomBytes()
    {
        // Arrange
        var client = _factory.CreateClient();

        var endpointResponse = await client.PostAsJsonAsync("/admin/api/endpoints", new
        {
            name = "Malformed Test",
            serviceName = "Test",
            protocol = 1,
            path = "/api/fault-malformed",
            httpMethod = "GET"
        });
        endpointResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var endpoint = await endpointResponse.Content.ReadFromJsonAsync<MockEndpoint>();

        var ruleResponse = await client.PostAsJsonAsync($"/admin/api/endpoints/{endpoint!.Id}/rules", new
        {
            ruleName = "Malformed Fault",
            priority = 1,
            conditions = Array.Empty<object>(),
            statusCode = 200,
            responseBody = "{}",
            faultType = (int)FaultType.MalformedResponse,
            faultConfig = "{\"byteCount\": 32}"
        });
        ruleResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Act
        var mockResponse = await client.GetAsync("/api/fault-malformed");

        // Assert
        mockResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var bytes = await mockResponse.Content.ReadAsByteArrayAsync();
        bytes.Length.Should().Be(32);
    }

    [Fact]
    [DisplayName("RandomDelay 故障後應繼續返回正常回應")]
    public async Task MockRequest_RandomDelayFault_ShouldStillReturnResponse()
    {
        // Arrange
        var client = _factory.CreateClient();

        var endpointResponse = await client.PostAsJsonAsync("/admin/api/endpoints", new
        {
            name = "Delay Test",
            serviceName = "Test",
            protocol = 1,
            path = "/api/fault-delay",
            httpMethod = "GET"
        });
        endpointResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var endpoint = await endpointResponse.Content.ReadFromJsonAsync<MockEndpoint>();

        var ruleResponse = await client.PostAsJsonAsync($"/admin/api/endpoints/{endpoint!.Id}/rules", new
        {
            ruleName = "Random Delay Fault",
            priority = 1,
            conditions = Array.Empty<object>(),
            statusCode = 200,
            responseBody = "{\"status\":\"ok\"}",
            faultType = (int)FaultType.RandomDelay,
            faultConfig = "{\"minDelayMs\": 1, \"maxDelayMs\": 10}"
        });
        ruleResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Act
        var mockResponse = await client.GetAsync("/api/fault-delay");

        // Assert
        mockResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await mockResponse.Content.ReadAsStringAsync();
        body.Should().Contain("ok");
    }
}
