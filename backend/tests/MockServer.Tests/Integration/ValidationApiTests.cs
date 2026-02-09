using System.ComponentModel;
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MockServer.Core.Entities;
using MockServer.Infrastructure.Data;
using Xunit;

namespace MockServer.Tests.Integration;

public class ValidationApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ValidationApiTests(WebApplicationFactory<Program> factory)
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
                    options.UseInMemoryDatabase("TestDb_Validation"));

                var sp = services.BuildServiceProvider();
                using var scope = sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<MockServerDbContext>();
                db.Database.EnsureCreated();
            });
        });
    }

    // ── Endpoint Validation ──

    [Fact]
    [DisplayName("建立 Endpoint 空名稱應返回 400")]
    public async Task CreateEndpoint_EmptyName_ShouldReturn400()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/admin/api/endpoints", new
        {
            name = "",
            serviceName = "Test",
            protocol = 1,
            path = "/api/empty-name",
            httpMethod = "GET"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Name is required");
    }

    [Fact]
    [DisplayName("建立 Endpoint 名稱超過 200 字元應返回 400")]
    public async Task CreateEndpoint_NameTooLong_ShouldReturn400()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/admin/api/endpoints", new
        {
            name = new string('X', 201),
            serviceName = "Test",
            protocol = 1,
            path = "/api/long-name",
            httpMethod = "GET"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("200 characters");
    }

    [Fact]
    [DisplayName("建立 Endpoint 無效 HttpMethod 應返回 400")]
    public async Task CreateEndpoint_InvalidMethod_ShouldReturn400()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/admin/api/endpoints", new
        {
            name = "Test",
            serviceName = "Test",
            protocol = 1,
            path = "/api/bad-method",
            httpMethod = "INVALID"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("HttpMethod must be one of");
    }

    [Fact]
    [DisplayName("建立 Endpoint 無效 protocol 應返回 400")]
    public async Task CreateEndpoint_InvalidProtocol_ShouldReturn400()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/admin/api/endpoints", new
        {
            name = "Test",
            serviceName = "Test",
            protocol = 99,
            path = "/api/bad-proto",
            httpMethod = "GET"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Protocol must be one of");
    }

    [Fact]
    [DisplayName("建立 Endpoint 無前斜線 path 應自動補正")]
    public async Task CreateEndpoint_PathWithoutSlash_ShouldAutoNormalize()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/admin/api/endpoints", new
        {
            name = "Auto Slash",
            serviceName = "Test",
            protocol = 1,
            path = "api/no-slash",
            httpMethod = "GET"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("/api/no-slash");
    }

    [Fact]
    [DisplayName("取得不存在 Endpoint 應返回 404 JSON")]
    public async Task GetEndpoint_NotFound_ShouldReturnJsonBody()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync($"/admin/api/endpoints/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Endpoint not found");
    }

    // ── Rule Validation ──

    [Fact]
    [DisplayName("建立 Rule delayMs 為負數應返回 400")]
    public async Task CreateRule_NegativeDelay_ShouldReturn400()
    {
        var client = _factory.CreateClient();

        // Create endpoint first
        var epResp = await client.PostAsJsonAsync("/admin/api/endpoints", new
        {
            name = "Rule Validation EP",
            serviceName = "Test",
            protocol = 1,
            path = "/api/rule-val",
            httpMethod = "GET"
        });
        var ep = await epResp.Content.ReadFromJsonAsync<MockEndpoint>();

        var response = await client.PostAsJsonAsync($"/admin/api/endpoints/{ep!.Id}/rules", new
        {
            ruleName = "Bad Delay",
            priority = 1,
            conditions = Array.Empty<object>(),
            statusCode = 200,
            responseBody = "{}",
            delayMs = -100
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("DelayMs must be 0 or greater");
    }

    [Fact]
    [DisplayName("建立 Rule 無效 faultType 應返回 400")]
    public async Task CreateRule_InvalidFaultType_ShouldReturn400()
    {
        var client = _factory.CreateClient();

        var epResp = await client.PostAsJsonAsync("/admin/api/endpoints", new
        {
            name = "Fault Type EP",
            serviceName = "Test",
            protocol = 1,
            path = "/api/fault-val",
            httpMethod = "GET"
        });
        var ep = await epResp.Content.ReadFromJsonAsync<MockEndpoint>();

        var response = await client.PostAsJsonAsync($"/admin/api/endpoints/{ep!.Id}/rules", new
        {
            ruleName = "Bad Fault",
            priority = 1,
            conditions = Array.Empty<object>(),
            statusCode = 200,
            responseBody = "{}",
            faultType = 99
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("FaultType must be one of");
    }

    [Fact]
    [DisplayName("建立 Rule 空名稱應返回 400")]
    public async Task CreateRule_EmptyName_ShouldReturn400()
    {
        var client = _factory.CreateClient();

        var epResp = await client.PostAsJsonAsync("/admin/api/endpoints", new
        {
            name = "Rule Name EP",
            serviceName = "Test",
            protocol = 1,
            path = "/api/rulename-val",
            httpMethod = "GET"
        });
        var ep = await epResp.Content.ReadFromJsonAsync<MockEndpoint>();

        var response = await client.PostAsJsonAsync($"/admin/api/endpoints/{ep!.Id}/rules", new
        {
            ruleName = "",
            priority = 1,
            conditions = Array.Empty<object>(),
            statusCode = 200,
            responseBody = "{}"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("RuleName is required");
    }

    // ── Proxy Config Validation ──

    [Fact]
    [DisplayName("建立 Proxy 無效 URL 應返回 400")]
    public async Task CreateProxyConfig_InvalidUrl_ShouldReturn400()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/admin/api/proxy-configs", new
        {
            targetBaseUrl = "not-a-url",
            timeoutMs = 10000
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("valid HTTP/HTTPS URL");
    }

    [Fact]
    [DisplayName("建立 Proxy 負數 timeoutMs 應返回 400")]
    public async Task CreateProxyConfig_NegativeTimeout_ShouldReturn400()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/admin/api/proxy-configs", new
        {
            targetBaseUrl = "https://example.com",
            timeoutMs = -1
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("TimeoutMs must be 0 or greater");
    }

    [Fact]
    [DisplayName("取得不存在 Proxy Config 應返回 404 JSON")]
    public async Task GetProxyConfig_NotFound_ShouldReturnJsonBody()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync($"/admin/api/proxy-configs/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Proxy config not found");
    }

    // ── Scenario 404 JSON ──

    [Fact]
    [DisplayName("取得不存在 Scenario 應返回 404 JSON")]
    public async Task GetScenario_NotFound_ShouldReturnJsonBody()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync($"/admin/api/scenarios/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Scenario not found");
    }
}
