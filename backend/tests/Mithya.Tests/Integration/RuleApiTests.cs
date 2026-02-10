using System.ComponentModel;
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mithya.Api.DTOs.Requests;
using Mithya.Core.Entities;
using Mithya.Core.Enums;
using Mithya.Infrastructure.Data;
using Xunit;

namespace Mithya.Tests.Integration;

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
                    d => d.ServiceType == typeof(DbContextOptions<MithyaDbContext>));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                services.AddDbContext<MithyaDbContext>(options =>
                {
                    options.UseInMemoryDatabase("TestDb_Rules");
                });

                var sp = services.BuildServiceProvider();
                using var scope = sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<MithyaDbContext>();
                db.Database.EnsureCreated();
            });
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
            Path = "/api/test-invalid-jsonpath",
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

    [Fact]
    [DisplayName("更新 Rule API 使用有效請求應該返回 200")]
    public async Task UpdateRule_ValidRequest_ShouldReturn200()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Create endpoint
        var endpointRequest = new CreateEndpointRequest
        {
            Name = "Test Endpoint",
            ServiceName = "Test Service",
            Protocol = ProtocolType.REST,
            Path = "/api/updaterule",
            HttpMethod = "POST"
        };

        var endpointResponse = await client.PostAsJsonAsync("/admin/api/endpoints", endpointRequest);
        var endpoint = await endpointResponse.Content.ReadFromJsonAsync<MockEndpoint>();

        // Create a rule
        var createRequest = new CreateRuleRequest
        {
            RuleName = "Original Rule",
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
            ResponseBody = "{\"status\":\"original\"}"
        };

        var createResponse = await client.PostAsJsonAsync($"/admin/api/endpoints/{endpoint!.Id}/rules", createRequest);
        var createdRule = await createResponse.Content.ReadFromJsonAsync<MockRule>();

        // Update the rule
        var updateRequest = new CreateRuleRequest
        {
            RuleName = "Updated Rule",
            Priority = 2,
            Conditions = new List<MatchCondition>
            {
                new MatchCondition
                {
                    SourceType = FieldSourceType.Body,
                    FieldPath = "$.userId",
                    Operator = MatchOperator.Equals,
                    Value = "456"
                }
            },
            StatusCode = 201,
            ResponseBody = "{\"status\":\"updated\"}",
            DelayMs = 100
        };

        // Act
        var response = await client.PutAsJsonAsync($"/admin/api/endpoints/{endpoint.Id}/rules/{createdRule!.Id}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updatedRule = await response.Content.ReadFromJsonAsync<MockRule>();
        updatedRule.Should().NotBeNull();
        updatedRule!.RuleName.Should().Be("Updated Rule");
        updatedRule.Priority.Should().Be(2);
        updatedRule.ResponseStatusCode.Should().Be(201);
        updatedRule.ResponseBody.Should().Be("{\"status\":\"updated\"}");
        updatedRule.DelayMs.Should().Be(100);
        updatedRule.Id.Should().Be(createdRule.Id); // Same ID
    }

    [Fact]
    [DisplayName("更新 Rule API 使用不存在的 Rule ID 應該返回 404")]
    public async Task UpdateRule_NonExistentRule_ShouldReturn404()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Create endpoint
        var endpointRequest = new CreateEndpointRequest
        {
            Name = "Test Endpoint",
            ServiceName = "Test Service",
            Protocol = ProtocolType.REST,
            Path = "/api/notfound",
            HttpMethod = "POST"
        };

        var endpointResponse = await client.PostAsJsonAsync("/admin/api/endpoints", endpointRequest);
        var endpoint = await endpointResponse.Content.ReadFromJsonAsync<MockEndpoint>();

        // Try to update non-existent rule
        var updateRequest = new CreateRuleRequest
        {
            RuleName = "Updated Rule",
            Priority = 1,
            Conditions = new List<MatchCondition>
            {
                new MatchCondition
                {
                    SourceType = FieldSourceType.Body,
                    FieldPath = "$.id",
                    Operator = MatchOperator.Equals,
                    Value = "123"
                }
            },
            StatusCode = 200,
            ResponseBody = "{}"
        };

        // Act
        var response = await client.PutAsJsonAsync($"/admin/api/endpoints/{endpoint!.Id}/rules/{Guid.NewGuid()}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    [DisplayName("更新 Rule API 使用無效 JsonPath 應該返回 400")]
    public async Task UpdateRule_InvalidJsonPath_ShouldReturn400()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Create endpoint
        var endpointRequest = new CreateEndpointRequest
        {
            Name = "Test Endpoint",
            ServiceName = "Test Service",
            Protocol = ProtocolType.REST,
            Path = "/api/invalidupdate",
            HttpMethod = "POST"
        };

        var endpointResponse = await client.PostAsJsonAsync("/admin/api/endpoints", endpointRequest);
        var endpoint = await endpointResponse.Content.ReadFromJsonAsync<MockEndpoint>();

        // Create a rule
        var createRequest = new CreateRuleRequest
        {
            RuleName = "Original Rule",
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
            ResponseBody = "{}"
        };

        var createResponse = await client.PostAsJsonAsync($"/admin/api/endpoints/{endpoint!.Id}/rules", createRequest);
        var createdRule = await createResponse.Content.ReadFromJsonAsync<MockRule>();

        // Try to update with invalid JsonPath
        var updateRequest = new CreateRuleRequest
        {
            RuleName = "Updated Rule",
            Priority = 1,
            Conditions = new List<MatchCondition>
            {
                new MatchCondition
                {
                    SourceType = FieldSourceType.Body,
                    FieldPath = "userId", // Should start with $.
                    Operator = MatchOperator.Equals,
                    Value = "456"
                }
            },
            StatusCode = 200,
            ResponseBody = "{}"
        };

        // Act
        var response = await client.PutAsJsonAsync($"/admin/api/endpoints/{endpoint.Id}/rules/{createdRule!.Id}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
