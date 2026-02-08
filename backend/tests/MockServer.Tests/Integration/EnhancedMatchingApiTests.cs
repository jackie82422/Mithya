using System.ComponentModel;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MockServer.Core.Entities;
using MockServer.Core.Enums;
using MockServer.Infrastructure.Data;
using Xunit;

namespace MockServer.Tests.Integration;

public class EnhancedMatchingApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public EnhancedMatchingApiTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<MockServerDbContext>));
                if (descriptor != null)
                    services.Remove(descriptor);

                services.AddDbContext<MockServerDbContext>(options =>
                    options.UseInMemoryDatabase("TestDb_EnhancedMatch"));

                var sp = services.BuildServiceProvider();
                using var scope = sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<MockServerDbContext>();
                db.Database.EnsureCreated();
            });
        });
    }

    [Fact]
    [DisplayName("建立 Rule 時應支援 LogicMode 欄位")]
    public async Task CreateRule_WithLogicMode_ShouldPersist()
    {
        // Arrange
        var client = _factory.CreateClient();

        var endpointResponse = await client.PostAsJsonAsync("/admin/api/endpoints", new
        {
            name = "Logic Mode Test",
            serviceName = "Test",
            protocol = 1,
            path = "/api/logic-mode",
            httpMethod = "GET"
        });
        endpointResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var endpoint = await endpointResponse.Content.ReadFromJsonAsync<MockEndpoint>();

        // Act
        var response = await client.PostAsJsonAsync($"/admin/api/endpoints/{endpoint!.Id}/rules", new
        {
            ruleName = "OR Logic Rule",
            priority = 1,
            conditions = new[]
            {
                new { sourceType = 2, fieldPath = "X-Test", @operator = 1, value = "a" },
                new { sourceType = 2, fieldPath = "X-Other", @operator = 1, value = "b" }
            },
            statusCode = 200,
            responseBody = "{\"logic\":\"or\"}",
            logicMode = (int)LogicMode.OR
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var rule = await response.Content.ReadFromJsonAsync<MockRule>();
        rule.Should().NotBeNull();
        rule!.LogicMode.Should().Be(LogicMode.OR);
    }

    [Fact]
    [DisplayName("OR 模式：任一條件匹配即應返回回應")]
    public async Task OrMode_AnyConditionMatches_ShouldReturnResponse()
    {
        // Arrange
        var client = _factory.CreateClient();

        var endpointResponse = await client.PostAsJsonAsync("/admin/api/endpoints", new
        {
            name = "OR Match Test",
            serviceName = "Test",
            protocol = 1,
            path = "/api/or-match",
            httpMethod = "GET"
        });
        endpointResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var endpoint = await endpointResponse.Content.ReadFromJsonAsync<MockEndpoint>();

        // Rule with OR logic: match if X-Role is "admin" OR X-Role is "superadmin"
        var ruleResponse = await client.PostAsJsonAsync($"/admin/api/endpoints/{endpoint!.Id}/rules", new
        {
            ruleName = "OR Rule",
            priority = 1,
            conditions = new[]
            {
                new { sourceType = 2, fieldPath = "X-Role", @operator = 1, value = "admin" },
                new { sourceType = 2, fieldPath = "X-Role", @operator = 1, value = "superadmin" }
            },
            statusCode = 200,
            responseBody = "{\"access\":\"granted\"}",
            logicMode = (int)LogicMode.OR
        });
        ruleResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Act - send with X-Role: superadmin (matches second condition)
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/or-match");
        request.Headers.Add("X-Role", "superadmin");
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("granted");
    }

    [Fact]
    [DisplayName("NotEquals 運算子應匹配不同值")]
    public async Task NotEquals_DifferentValue_ShouldMatch()
    {
        // Arrange
        var client = _factory.CreateClient();

        var endpointResponse = await client.PostAsJsonAsync("/admin/api/endpoints", new
        {
            name = "NotEquals Test",
            serviceName = "Test",
            protocol = 1,
            path = "/api/not-equals",
            httpMethod = "GET"
        });
        endpointResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var endpoint = await endpointResponse.Content.ReadFromJsonAsync<MockEndpoint>();

        // Rule: match when X-Env is NOT "production"
        var ruleResponse = await client.PostAsJsonAsync($"/admin/api/endpoints/{endpoint!.Id}/rules", new
        {
            ruleName = "NotEquals Rule",
            priority = 1,
            conditions = new[]
            {
                new { sourceType = 2, fieldPath = "X-Env", @operator = (int)MatchOperator.NotEquals, value = "production" }
            },
            statusCode = 200,
            responseBody = "{\"env\":\"non-prod\"}"
        });
        ruleResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Act - send with X-Env: staging
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/not-equals");
        request.Headers.Add("X-Env", "staging");
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("non-prod");
    }

    [Fact]
    [DisplayName("JsonSchema 驗證應匹配符合 schema 的請求")]
    public async Task JsonSchema_ValidBody_ShouldMatch()
    {
        // Arrange
        var client = _factory.CreateClient();

        var endpointResponse = await client.PostAsJsonAsync("/admin/api/endpoints", new
        {
            name = "Schema Test",
            serviceName = "Test",
            protocol = 1,
            path = "/api/schema-test",
            httpMethod = "POST"
        });
        endpointResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var endpoint = await endpointResponse.Content.ReadFromJsonAsync<MockEndpoint>();

        // Rule: match when body conforms to schema
        var schema = """{"type":"object","properties":{"name":{"type":"string"}},"required":["name"]}""";
        var ruleResponse = await client.PostAsJsonAsync($"/admin/api/endpoints/{endpoint!.Id}/rules", new
        {
            ruleName = "Schema Rule",
            priority = 1,
            conditions = new[]
            {
                new { sourceType = 1, fieldPath = "$", @operator = (int)MatchOperator.JsonSchema, value = schema }
            },
            statusCode = 200,
            responseBody = "{\"valid\":true}"
        });
        ruleResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Act
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/schema-test");
        request.Content = new StringContent("{\"name\":\"John\"}", Encoding.UTF8, "application/json");
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("valid");
    }
}
