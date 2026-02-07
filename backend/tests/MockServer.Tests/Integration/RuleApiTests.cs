using System.ComponentModel;
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MockServer.Api.DTOs.Requests;
using MockServer.Core.Entities;
using MockServer.Core.Enums;
using MockServer.Infrastructure.Data;
using Xunit;

namespace MockServer.Tests.Integration;

public class RuleApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public RuleApiTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<MockServerDbContext>));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                services.AddDbContext<MockServerDbContext>(options =>
                {
                    options.UseInMemoryDatabase("TestDb_Rules");
                });

                var sp = services.BuildServiceProvider();
                using var scope = sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<MockServerDbContext>();
                db.Database.EnsureCreated();
            });

            builder.UseSetting("WireMock:Port", "0");
        });
    }

    [Fact]
    [DisplayName("建立 Rule API 使用有效請求應該返回 201")]
    public async Task CreateRule_ValidRequest_ShouldReturn201()
    {
        // Arrange
        var client = _factory.CreateClient();

        // First create an endpoint
        var endpointRequest = new CreateEndpointRequest
        {
            Name = "Test Endpoint",
            ServiceName = "Test Service",
            Protocol = ProtocolType.REST,
            Path = "/api/test",
            HttpMethod = "POST"
        };

        var endpointResponse = await client.PostAsJsonAsync("/admin/api/endpoints", endpointRequest);
        var endpoint = await endpointResponse.Content.ReadFromJsonAsync<MockEndpoint>();

        // Create a rule
        var ruleRequest = new CreateRuleRequest
        {
            RuleName = "Test Rule",
            Priority = 1,
            Conditions = new List<MatchCondition>
            {
                new MatchCondition
                {
                    SourceType = FieldSourceType.Body,
                    FieldPath = "$.userId",
                    Operator = MatchOperator.Equals,
                    Value = "123"
                }
            },
            StatusCode = 200,
            ResponseBody = "{\"status\":\"success\"}",
            DelayMs = 0
        };

        // Act
        var response = await client.PostAsJsonAsync($"/admin/api/endpoints/{endpoint!.Id}/rules", ruleRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var rule = await response.Content.ReadFromJsonAsync<MockRule>();
        rule.Should().NotBeNull();
        rule!.RuleName.Should().Be("Test Rule");
        rule.Priority.Should().Be(1);
    }

    [Fact]
    [DisplayName("建立 Rule API 使用無效 JsonPath 應該返回 400")]
    public async Task CreateRule_InvalidJsonPath_ShouldReturn400()
    {
        // Arrange
        var client = _factory.CreateClient();

        // First create an endpoint
        var endpointRequest = new CreateEndpointRequest
        {
            Name = "Test Endpoint",
            ServiceName = "Test Service",
            Protocol = ProtocolType.REST,
            Path = "/api/test",
            HttpMethod = "POST"
        };

        var endpointResponse = await client.PostAsJsonAsync("/admin/api/endpoints", endpointRequest);
        var endpoint = await endpointResponse.Content.ReadFromJsonAsync<MockEndpoint>();

        // Create a rule with invalid JsonPath
        var ruleRequest = new CreateRuleRequest
        {
            RuleName = "Invalid Rule",
            Priority = 1,
            Conditions = new List<MatchCondition>
            {
                new MatchCondition
                {
                    SourceType = FieldSourceType.Body,
                    FieldPath = "userId", // Should start with $.
                    Operator = MatchOperator.Equals,
                    Value = "123"
                }
            },
            StatusCode = 200,
            ResponseBody = "{\"status\":\"success\"}"
        };

        // Act
        var response = await client.PostAsJsonAsync($"/admin/api/endpoints/{endpoint!.Id}/rules", ruleRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    [DisplayName("根據 Endpoint 取得 Rule API 應該返回 Rule 列表")]
    public async Task GetRulesByEndpoint_ShouldReturnRules()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Create endpoint
        var endpointRequest = new CreateEndpointRequest
        {
            Name = "Test Endpoint",
            ServiceName = "Test Service",
            Protocol = ProtocolType.REST,
            Path = "/api/getrules",
            HttpMethod = "POST"
        };

        var endpointResponse = await client.PostAsJsonAsync("/admin/api/endpoints", endpointRequest);
        var endpoint = await endpointResponse.Content.ReadFromJsonAsync<MockEndpoint>();

        // Create multiple rules
        var rule1 = new CreateRuleRequest
        {
            RuleName = "Rule 1",
            Priority = 1,
            Conditions = new List<MatchCondition>
            {
                new MatchCondition
                {
                    SourceType = FieldSourceType.Body,
                    FieldPath = "$.id",
                    Operator = MatchOperator.Equals,
                    Value = "1"
                }
            },
            StatusCode = 200,
            ResponseBody = "{\"rule\":1}"
        };

        var rule2 = new CreateRuleRequest
        {
            RuleName = "Rule 2",
            Priority = 2,
            Conditions = new List<MatchCondition>
            {
                new MatchCondition
                {
                    SourceType = FieldSourceType.Body,
                    FieldPath = "$.id",
                    Operator = MatchOperator.Equals,
                    Value = "2"
                }
            },
            StatusCode = 200,
            ResponseBody = "{\"rule\":2}"
        };

        await client.PostAsJsonAsync($"/admin/api/endpoints/{endpoint!.Id}/rules", rule1);
        await client.PostAsJsonAsync($"/admin/api/endpoints/{endpoint.Id}/rules", rule2);

        // Act
        var response = await client.GetAsync($"/admin/api/endpoints/{endpoint.Id}/rules");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var rules = await response.Content.ReadFromJsonAsync<List<MockRule>>();
        rules.Should().NotBeNull();
        rules.Should().HaveCount(2);
    }
}
