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

public class ProxyConfigApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ProxyConfigApiTests(WebApplicationFactory<Program> factory)
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
                    options.UseInMemoryDatabase("TestDb_ProxyConfig"));

                var sp = services.BuildServiceProvider();
                using var scope = sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<MockServerDbContext>();
                db.Database.EnsureCreated();
            });
        });
    }

    [Fact]
    [DisplayName("建立 Proxy Config 應返回 201")]
    public async Task CreateProxyConfig_ShouldReturn201()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/admin/api/proxy-configs", new
        {
            targetBaseUrl = "https://api.example.com",
            isActive = true,
            isRecording = false,
            forwardHeaders = true,
            timeoutMs = 5000
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var config = await response.Content.ReadFromJsonAsync<ProxyConfig>();
        config.Should().NotBeNull();
        config!.TargetBaseUrl.Should().Be("https://api.example.com");
        config.TimeoutMs.Should().Be(5000);
    }

    [Fact]
    [DisplayName("建立 Proxy Config 缺少 TargetBaseUrl 應返回 400")]
    public async Task CreateProxyConfig_MissingUrl_ShouldReturn400()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/admin/api/proxy-configs", new
        {
            targetBaseUrl = "",
            isActive = true
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    [DisplayName("取得所有 Proxy Config 應返回列表")]
    public async Task GetAllProxyConfigs_ShouldReturnList()
    {
        // Arrange
        var client = _factory.CreateClient();

        await client.PostAsJsonAsync("/admin/api/proxy-configs", new
        {
            targetBaseUrl = "https://list-test.example.com",
            isActive = true
        });

        // Act
        var response = await client.GetAsync("/admin/api/proxy-configs");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var configs = await response.Content.ReadFromJsonAsync<List<ProxyConfig>>();
        configs.Should().NotBeNull();
        configs!.Should().Contain(c => c.TargetBaseUrl == "https://list-test.example.com");
    }

    [Fact]
    [DisplayName("Toggle Proxy Config 應切換 IsActive")]
    public async Task ToggleProxyConfig_ShouldToggleIsActive()
    {
        // Arrange
        var client = _factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync("/admin/api/proxy-configs", new
        {
            targetBaseUrl = "https://toggle-test.example.com",
            isActive = true
        });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<ProxyConfig>();

        // Act
        var patchRequest = new HttpRequestMessage(HttpMethod.Patch,
            $"/admin/api/proxy-configs/{created!.Id}/toggle");
        var response = await client.SendAsync(patchRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var toggled = await response.Content.ReadFromJsonAsync<ProxyConfig>();
        toggled!.IsActive.Should().BeFalse();
    }

    [Fact]
    [DisplayName("Toggle Recording 應切換 IsRecording")]
    public async Task ToggleRecording_ShouldToggleIsRecording()
    {
        // Arrange
        var client = _factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync("/admin/api/proxy-configs", new
        {
            targetBaseUrl = "https://recording-test.example.com",
            isActive = true,
            isRecording = false
        });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<ProxyConfig>();

        // Act
        var patchRequest = new HttpRequestMessage(HttpMethod.Patch,
            $"/admin/api/proxy-configs/{created!.Id}/toggle-recording");
        var response = await client.SendAsync(patchRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var toggled = await response.Content.ReadFromJsonAsync<ProxyConfig>();
        toggled!.IsRecording.Should().BeTrue();
    }

    [Fact]
    [DisplayName("更新 Proxy Config 應正確修改")]
    public async Task UpdateProxyConfig_ShouldModifyFields()
    {
        // Arrange
        var client = _factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync("/admin/api/proxy-configs", new
        {
            targetBaseUrl = "https://update-test.example.com",
            isActive = true,
            timeoutMs = 5000
        });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<ProxyConfig>();

        // Act
        var response = await client.PutAsJsonAsync($"/admin/api/proxy-configs/{created!.Id}", new
        {
            targetBaseUrl = "https://updated.example.com",
            isActive = true,
            isRecording = true,
            forwardHeaders = false,
            timeoutMs = 15000
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<ProxyConfig>();
        updated!.TargetBaseUrl.Should().Be("https://updated.example.com");
        updated.TimeoutMs.Should().Be(15000);
        updated.ForwardHeaders.Should().BeFalse();
        updated.IsRecording.Should().BeTrue();
    }

    [Fact]
    [DisplayName("刪除 Proxy Config 應返回 204")]
    public async Task DeleteProxyConfig_ShouldReturn204()
    {
        // Arrange
        var client = _factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync("/admin/api/proxy-configs", new
        {
            targetBaseUrl = "https://delete-test.example.com",
            isActive = true
        });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<ProxyConfig>();

        // Act
        var response = await client.DeleteAsync($"/admin/api/proxy-configs/{created!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify it's gone
        var getResponse = await client.GetAsync($"/admin/api/proxy-configs/{created.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    [DisplayName("不存在的 Proxy Config 應返回 404")]
    public async Task GetNonExistentProxyConfig_ShouldReturn404()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync($"/admin/api/proxy-configs/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
