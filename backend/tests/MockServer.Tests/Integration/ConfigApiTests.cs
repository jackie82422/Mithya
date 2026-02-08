using System.ComponentModel;
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MockServer.Infrastructure.Data;
using Xunit;

namespace MockServer.Tests.Integration;

public class ConfigApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ConfigApiTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove existing DbContext
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<MockServerDbContext>));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                // Add InMemory database for testing
                services.AddDbContext<MockServerDbContext>(options =>
                {
                    options.UseInMemoryDatabase("TestDb_Config");
                });

                // Build service provider and create database
                var sp = services.BuildServiceProvider();
                using var scope = sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<MockServerDbContext>();
                db.Database.EnsureCreated();
            });
        });
    }

    [Fact]
    [DisplayName("取得伺服器配置 API 應該返回 Mock Server 資訊")]
    public async Task GetConfig_ShouldReturnServerConfiguration()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/admin/api/config");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var config = await response.Content.ReadFromJsonAsync<ServerConfig>();
        config.Should().NotBeNull();
        config!.MockServerUrl.Should().NotBeNullOrEmpty();
        config.MockServerHost.Should().NotBeNullOrEmpty();
        config.AdminApiUrl.Should().NotBeNullOrEmpty();
    }

    [Fact]
    [DisplayName("取得伺服器配置 API Mock Server URL 和 Admin API URL 應該相同（同 port）")]
    public async Task GetConfig_MockServerAndAdminShouldUseSameUrl()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/admin/api/config");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var config = await response.Content.ReadFromJsonAsync<ServerConfig>();
        config.Should().NotBeNull();
        config!.MockServerUrl.Should().Be(config.AdminApiUrl);
    }

    [Fact]
    [DisplayName("取得伺服器配置 API 應該包含正確的 URL scheme")]
    public async Task GetConfig_ShouldIncludeCorrectUrlScheme()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/admin/api/config");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var config = await response.Content.ReadFromJsonAsync<ServerConfig>();
        config.Should().NotBeNull();
        config!.MockServerUrl.Should().StartWith("http://");
        config.AdminApiUrl.Should().StartWith("http://");
    }
}

// DTO for deserialization
public class ServerConfig
{
    public int MockServerPort { get; set; }
    public string MockServerUrl { get; set; } = string.Empty;
    public string MockServerHost { get; set; } = string.Empty;
    public string AdminApiUrl { get; set; } = string.Empty;
}
