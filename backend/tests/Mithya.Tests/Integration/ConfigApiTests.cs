using System.ComponentModel;
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mithya.Infrastructure.Data;
using Xunit;

namespace Mithya.Tests.Integration;

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
                    d => d.ServiceType == typeof(DbContextOptions<MithyaDbContext>));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                // Add InMemory database for testing
                services.AddDbContext<MithyaDbContext>(options =>
                {
                    options.UseInMemoryDatabase("TestDb_Config");
                });

                // Build service provider and create database
                var sp = services.BuildServiceProvider();
                using var scope = sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<MithyaDbContext>();
                db.Database.EnsureCreated();
            });
        });
    }

    [Fact]
    [DisplayName("取得伺服器配置 API 應該返回 Mithya 資訊")]
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
        config!.MithyaUrl.Should().NotBeNullOrEmpty();
        config.MithyaHost.Should().NotBeNullOrEmpty();
        config.AdminApiUrl.Should().NotBeNullOrEmpty();
    }

    [Fact]
    [DisplayName("取得伺服器配置 API Mithya URL 和 Admin API URL 應該相同（同 port）")]
    public async Task GetConfig_MithyaAndAdminShouldUseSameUrl()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/admin/api/config");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var config = await response.Content.ReadFromJsonAsync<ServerConfig>();
        config.Should().NotBeNull();
        config!.MithyaUrl.Should().Be(config.AdminApiUrl);
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
        config!.MithyaUrl.Should().StartWith("http://");
        config.AdminApiUrl.Should().StartWith("http://");
    }
}

// DTO for deserialization
public class ServerConfig
{
    public int MithyaPort { get; set; }
    public string MithyaUrl { get; set; } = string.Empty;
    public string MithyaHost { get; set; } = string.Empty;
    public string AdminApiUrl { get; set; } = string.Empty;
}
