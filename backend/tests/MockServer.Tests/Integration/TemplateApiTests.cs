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

public class TemplateApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public TemplateApiTests(WebApplicationFactory<Program> factory)
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
                    options.UseInMemoryDatabase("TestDb_Template"));

                var sp = services.BuildServiceProvider();
                using var scope = sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<MockServerDbContext>();
                db.Database.EnsureCreated();
            });
        });
    }

    [Fact]
    [DisplayName("模板預覽 API 應正確渲染 Handlebars 模板")]
    public async Task PreviewTemplate_ValidTemplate_ShouldReturnRendered()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new
        {
            template = "Hello {{Request.PathParams.name}}, today is {{now \"yyyy-MM-dd\"}}",
            mockRequest = new
            {
                method = "GET",
                path = "/api/greet/John",
                pathParams = new Dictionary<string, string> { { "name", "John" } }
            }
        };

        // Act
        var response = await client.PostAsJsonAsync("/admin/api/templates/preview", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Hello John");
        content.Should().Contain(DateTime.UtcNow.ToString("yyyy-MM-dd"));
    }

    [Fact]
    [DisplayName("模板預覽 API 傳入無效模板應返回錯誤訊息")]
    public async Task PreviewTemplate_InvalidTemplate_ShouldReturnError()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new
        {
            template = "{{#if}}missing block{{/unless}}",
            mockRequest = new { method = "GET", path = "/" }
        };

        // Act
        var response = await client.PostAsJsonAsync("/admin/api/templates/preview", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("error");
    }

    [Fact]
    [DisplayName("啟用模板的 Rule 應動態渲染回應 Body")]
    public async Task MockRequest_TemplateRule_ShouldRenderDynamicResponse()
    {
        // Arrange
        var client = _factory.CreateClient();

        var endpointResponse = await client.PostAsJsonAsync("/admin/api/endpoints", new
        {
            name = "Template Dynamic Test",
            serviceName = "Test",
            protocol = 1,
            path = "/api/tpl-greet/{name}",
            httpMethod = "GET"
        });
        endpointResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var endpoint = await endpointResponse.Content.ReadFromJsonAsync<MockEndpoint>();

        var ruleResponse = await client.PostAsJsonAsync($"/admin/api/endpoints/{endpoint!.Id}/rules", new
        {
            ruleName = "Greeting Template",
            priority = 1,
            conditions = Array.Empty<object>(),
            statusCode = 200,
            responseBody = "{\"message\": \"Hello {{Request.PathParams.name}}\", \"method\": \"{{Request.Method}}\"}",
            isTemplate = true
        });
        ruleResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Act
        var mockResponse = await client.GetAsync("/api/tpl-greet/Alice");

        // Assert
        mockResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await mockResponse.Content.ReadAsStringAsync();
        body.Should().Contain("Hello Alice");
        body.Should().Contain("GET");
    }

    [Fact]
    [DisplayName("建立 Rule 時應支援 IsTemplate 欄位")]
    public async Task CreateRule_WithTemplateFlag_ShouldPersist()
    {
        // Arrange
        var client = _factory.CreateClient();

        var endpointResponse = await client.PostAsJsonAsync("/admin/api/endpoints", new
        {
            name = "Template Persist Test",
            serviceName = "Test",
            protocol = 1,
            path = "/api/tpl-persist",
            httpMethod = "GET"
        });
        endpointResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var endpoint = await endpointResponse.Content.ReadFromJsonAsync<MockEndpoint>();

        // Act
        var response = await client.PostAsJsonAsync($"/admin/api/endpoints/{endpoint!.Id}/rules", new
        {
            ruleName = "Template Rule",
            priority = 1,
            conditions = Array.Empty<object>(),
            statusCode = 200,
            responseBody = "{{uuid}}",
            isTemplate = true,
            isResponseHeadersTemplate = true
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var rule = await response.Content.ReadFromJsonAsync<MockRule>();
        rule.Should().NotBeNull();
        rule!.IsTemplate.Should().BeTrue();
        rule.IsResponseHeadersTemplate.Should().BeTrue();
    }
}
