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

public class ServiceProxyApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ServiceProxyApiTests(WebApplicationFactory<Program> factory)
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
                    options.UseInMemoryDatabase("TestDb_ServiceProxy"));

                var sp = services.BuildServiceProvider();
                using var scope = sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<MockServerDbContext>();
                db.Database.EnsureCreated();
            });
        });
    }

    private async Task<MockEndpoint> CreateEndpointWithService(HttpClient client, string serviceName)
    {
        var response = await client.PostAsJsonAsync("/admin/api/endpoints", new
        {
            name = $"{serviceName} Endpoint",
            serviceName,
            protocol = 1,
            path = $"/api/{serviceName.ToLower()}/test",
            httpMethod = "GET"
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<MockEndpoint>())!;
    }

    [Fact]
    [DisplayName("建立 Service Proxy 應返回 201")]
    public async Task CreateServiceProxy_ShouldReturn201()
    {
        // Arrange
        var client = _factory.CreateClient();
        await CreateEndpointWithService(client, "UserService");

        // Act
        var response = await client.PostAsJsonAsync("/admin/api/service-proxies", new
        {
            serviceName = "UserService",
            targetBaseUrl = "https://api.example.com",
            isActive = true,
            forwardHeaders = true,
            timeoutMs = 5000,
            fallbackEnabled = true
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var proxy = await response.Content.ReadFromJsonAsync<ServiceProxy>();
        proxy.Should().NotBeNull();
        proxy!.ServiceName.Should().Be("UserService");
        proxy.TargetBaseUrl.Should().Be("https://api.example.com");
        proxy.FallbackEnabled.Should().BeTrue();
    }

    [Fact]
    [DisplayName("建立 Service Proxy 缺少 ServiceName 應返回 400")]
    public async Task CreateServiceProxy_MissingServiceName_ShouldReturn400()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/admin/api/service-proxies", new
        {
            serviceName = "",
            targetBaseUrl = "https://api.example.com"
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    [DisplayName("建立 Service Proxy 缺少 TargetBaseUrl 應返回 400")]
    public async Task CreateServiceProxy_MissingTargetBaseUrl_ShouldReturn400()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/admin/api/service-proxies", new
        {
            serviceName = "TestService",
            targetBaseUrl = ""
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    [DisplayName("建立 Service Proxy 無效 URL 應返回 400")]
    public async Task CreateServiceProxy_InvalidUrl_ShouldReturn400()
    {
        // Arrange
        var client = _factory.CreateClient();
        await CreateEndpointWithService(client, "UrlTestService");

        // Act
        var response = await client.PostAsJsonAsync("/admin/api/service-proxies", new
        {
            serviceName = "UrlTestService",
            targetBaseUrl = "not-a-url"
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    [DisplayName("建立 Service Proxy 不存在的 ServiceName 應返回 400")]
    public async Task CreateServiceProxy_NonExistentService_ShouldReturn400()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/admin/api/service-proxies", new
        {
            serviceName = "NonExistentService",
            targetBaseUrl = "https://api.example.com"
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("No endpoints found");
    }

    [Fact]
    [DisplayName("建立重複 ServiceName 的 Proxy 應返回 400")]
    public async Task CreateServiceProxy_DuplicateServiceName_ShouldReturn400()
    {
        // Arrange
        var client = _factory.CreateClient();
        await CreateEndpointWithService(client, "DupService");

        await client.PostAsJsonAsync("/admin/api/service-proxies", new
        {
            serviceName = "DupService",
            targetBaseUrl = "https://api.example.com",
            isActive = true
        });

        // Act
        var response = await client.PostAsJsonAsync("/admin/api/service-proxies", new
        {
            serviceName = "DupService",
            targetBaseUrl = "https://api2.example.com",
            isActive = true
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("already exists");
    }

    [Fact]
    [DisplayName("取得所有 Service Proxy 應返回列表")]
    public async Task GetAllServiceProxies_ShouldReturnList()
    {
        // Arrange
        var client = _factory.CreateClient();
        await CreateEndpointWithService(client, "ListService");

        await client.PostAsJsonAsync("/admin/api/service-proxies", new
        {
            serviceName = "ListService",
            targetBaseUrl = "https://list.example.com",
            isActive = true
        });

        // Act
        var response = await client.GetAsync("/admin/api/service-proxies");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var proxies = await response.Content.ReadFromJsonAsync<List<ServiceProxy>>();
        proxies.Should().NotBeNull();
        proxies!.Should().Contain(p => p.ServiceName == "ListService");
    }

    [Fact]
    [DisplayName("根據 ID 取得 Service Proxy 應返回資料")]
    public async Task GetServiceProxyById_ShouldReturnProxy()
    {
        // Arrange
        var client = _factory.CreateClient();
        await CreateEndpointWithService(client, "GetByIdService");

        var createResponse = await client.PostAsJsonAsync("/admin/api/service-proxies", new
        {
            serviceName = "GetByIdService",
            targetBaseUrl = "https://getbyid.example.com",
            isActive = true
        });
        var created = await createResponse.Content.ReadFromJsonAsync<ServiceProxy>();

        // Act
        var response = await client.GetAsync($"/admin/api/service-proxies/{created!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var proxy = await response.Content.ReadFromJsonAsync<ServiceProxy>();
        proxy!.ServiceName.Should().Be("GetByIdService");
    }

    [Fact]
    [DisplayName("不存在的 Service Proxy ID 應返回 404")]
    public async Task GetNonExistentServiceProxy_ShouldReturn404()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync($"/admin/api/service-proxies/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    [DisplayName("根據 ServiceName 取得 Service Proxy")]
    public async Task GetByServiceName_ShouldReturnProxy()
    {
        // Arrange
        var client = _factory.CreateClient();
        await CreateEndpointWithService(client, "ByNameService");

        await client.PostAsJsonAsync("/admin/api/service-proxies", new
        {
            serviceName = "ByNameService",
            targetBaseUrl = "https://byname.example.com",
            isActive = true
        });

        // Act
        var response = await client.GetAsync("/admin/api/service-proxies/by-service/ByNameService");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var proxy = await response.Content.ReadFromJsonAsync<ServiceProxy>();
        proxy!.TargetBaseUrl.Should().Be("https://byname.example.com");
    }

    [Fact]
    [DisplayName("更新 Service Proxy 應正確修改")]
    public async Task UpdateServiceProxy_ShouldModifyFields()
    {
        // Arrange
        var client = _factory.CreateClient();
        await CreateEndpointWithService(client, "UpdateService");

        var createResponse = await client.PostAsJsonAsync("/admin/api/service-proxies", new
        {
            serviceName = "UpdateService",
            targetBaseUrl = "https://original.example.com",
            isActive = true,
            timeoutMs = 5000
        });
        var created = await createResponse.Content.ReadFromJsonAsync<ServiceProxy>();

        // Act
        var response = await client.PutAsJsonAsync($"/admin/api/service-proxies/{created!.Id}", new
        {
            serviceName = "UpdateService",
            targetBaseUrl = "https://updated.example.com",
            isActive = true,
            forwardHeaders = false,
            timeoutMs = 15000,
            fallbackEnabled = false
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<ServiceProxy>();
        updated!.TargetBaseUrl.Should().Be("https://updated.example.com");
        updated.TimeoutMs.Should().Be(15000);
        updated.ForwardHeaders.Should().BeFalse();
        updated.FallbackEnabled.Should().BeFalse();
    }

    [Fact]
    [DisplayName("刪除 Service Proxy 應返回 204")]
    public async Task DeleteServiceProxy_ShouldReturn204()
    {
        // Arrange
        var client = _factory.CreateClient();
        await CreateEndpointWithService(client, "DeleteService");

        var createResponse = await client.PostAsJsonAsync("/admin/api/service-proxies", new
        {
            serviceName = "DeleteService",
            targetBaseUrl = "https://delete.example.com",
            isActive = true
        });
        var created = await createResponse.Content.ReadFromJsonAsync<ServiceProxy>();

        // Act
        var response = await client.DeleteAsync($"/admin/api/service-proxies/{created!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await client.GetAsync($"/admin/api/service-proxies/{created.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    [DisplayName("Toggle Service Proxy 應切換 IsActive")]
    public async Task ToggleServiceProxy_ShouldToggleIsActive()
    {
        // Arrange
        var client = _factory.CreateClient();
        await CreateEndpointWithService(client, "ToggleService");

        var createResponse = await client.PostAsJsonAsync("/admin/api/service-proxies", new
        {
            serviceName = "ToggleService",
            targetBaseUrl = "https://toggle.example.com",
            isActive = true
        });
        var created = await createResponse.Content.ReadFromJsonAsync<ServiceProxy>();

        // Act
        var patchRequest = new HttpRequestMessage(HttpMethod.Patch,
            $"/admin/api/service-proxies/{created!.Id}/toggle");
        var response = await client.SendAsync(patchRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var toggled = await response.Content.ReadFromJsonAsync<ServiceProxy>();
        toggled!.IsActive.Should().BeFalse();
    }

    [Fact]
    [DisplayName("Toggle Recording 應切換 IsRecording")]
    public async Task ToggleRecording_ShouldToggleIsRecording()
    {
        // Arrange
        var client = _factory.CreateClient();
        await CreateEndpointWithService(client, "RecordService");

        var createResponse = await client.PostAsJsonAsync("/admin/api/service-proxies", new
        {
            serviceName = "RecordService",
            targetBaseUrl = "https://record.example.com",
            isActive = true,
            isRecording = false
        });
        var created = await createResponse.Content.ReadFromJsonAsync<ServiceProxy>();

        // Act
        var patchRequest = new HttpRequestMessage(HttpMethod.Patch,
            $"/admin/api/service-proxies/{created!.Id}/toggle-recording");
        var response = await client.SendAsync(patchRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var toggled = await response.Content.ReadFromJsonAsync<ServiceProxy>();
        toggled!.IsRecording.Should().BeTrue();
    }

    [Fact]
    [DisplayName("Toggle Fallback 應切換 FallbackEnabled")]
    public async Task ToggleFallback_ShouldToggleFallbackEnabled()
    {
        // Arrange
        var client = _factory.CreateClient();
        await CreateEndpointWithService(client, "FallbackService");

        var createResponse = await client.PostAsJsonAsync("/admin/api/service-proxies", new
        {
            serviceName = "FallbackService",
            targetBaseUrl = "https://fallback.example.com",
            isActive = true,
            fallbackEnabled = true
        });
        var created = await createResponse.Content.ReadFromJsonAsync<ServiceProxy>();

        // Act
        var patchRequest = new HttpRequestMessage(HttpMethod.Patch,
            $"/admin/api/service-proxies/{created!.Id}/toggle-fallback");
        var response = await client.SendAsync(patchRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var toggled = await response.Content.ReadFromJsonAsync<ServiceProxy>();
        toggled!.FallbackEnabled.Should().BeFalse();
    }

    [Fact]
    [DisplayName("取得可用 Services 列表應返回 endpoint 統計")]
    public async Task GetServices_ShouldReturnServiceStats()
    {
        // Arrange
        var client = _factory.CreateClient();
        await CreateEndpointWithService(client, "StatsService");

        // Act
        var response = await client.GetAsync("/admin/api/service-proxies/services");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("StatsService");
        body.Should().Contain("endpointCount");
        body.Should().Contain("hasProxy");
    }
}
