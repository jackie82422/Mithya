using System.ComponentModel;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mithya.Core.Entities;
using Mithya.Infrastructure.Data;
using Mithya.Infrastructure.MockEngine;
using Xunit;

namespace Mithya.Tests.Unit;

public class ProxyConfigCacheTests
{
    private IServiceScopeFactory BuildScopeFactory(string dbName, params ProxyConfig[] configs)
    {
        var services = new ServiceCollection();
        services.AddDbContext<MithyaDbContext>(options =>
            options.UseInMemoryDatabase(dbName));

        services.AddScoped<Core.Interfaces.IProxyConfigRepository,
            Infrastructure.Repositories.ProxyConfigRepository>();

        var sp = services.BuildServiceProvider();

        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MithyaDbContext>();
        db.Database.EnsureCreated();
        foreach (var c in configs)
        {
            c.Id = Guid.NewGuid();
            c.CreatedAt = DateTime.UtcNow;
            c.UpdatedAt = DateTime.UtcNow;
            db.ProxyConfigs.Add(c);
        }
        db.SaveChanges();

        return sp.GetRequiredService<IServiceScopeFactory>();
    }

    [Fact]
    [DisplayName("LoadAllAsync 應載入全域 Proxy 設定")]
    public async Task LoadAll_ShouldLoadGlobalConfig()
    {
        // Arrange
        var factory = BuildScopeFactory("ProxyCacheTest_Global", new ProxyConfig
        {
            TargetBaseUrl = "https://api.example.com",
            IsActive = true,
            EndpointId = null
        });
        var cache = new ProxyConfigCache(factory);

        // Act
        await cache.LoadAllAsync();

        // Assert
        cache.GetGlobalActive().Should().NotBeNull();
        cache.GetGlobalActive()!.TargetBaseUrl.Should().Be("https://api.example.com");
    }

    [Fact]
    [DisplayName("LoadAllAsync 不應載入非啟用的設定")]
    public async Task LoadAll_ShouldSkipInactiveConfigs()
    {
        // Arrange
        var factory = BuildScopeFactory("ProxyCacheTest_Inactive", new ProxyConfig
        {
            TargetBaseUrl = "https://inactive.example.com",
            IsActive = false,
            EndpointId = null
        });
        var cache = new ProxyConfigCache(factory);

        // Act
        await cache.LoadAllAsync();

        // Assert
        cache.GetGlobalActive().Should().BeNull();
    }

    [Fact]
    [DisplayName("LoadAllAsync 應載入 Endpoint 專屬 Proxy 設定")]
    public async Task LoadAll_ShouldLoadEndpointConfig()
    {
        // Arrange
        var endpointId = Guid.NewGuid();
        var factory = BuildScopeFactory("ProxyCacheTest_Endpoint", new ProxyConfig
        {
            TargetBaseUrl = "https://endpoint.example.com",
            IsActive = true,
            EndpointId = endpointId
        });
        var cache = new ProxyConfigCache(factory);

        // Act
        await cache.LoadAllAsync();

        // Assert
        cache.GetActiveForEndpoint(endpointId).Should().NotBeNull();
        cache.GetActiveForEndpoint(endpointId)!.TargetBaseUrl.Should().Be("https://endpoint.example.com");
        cache.GetGlobalActive().Should().BeNull();
    }

    [Fact]
    [DisplayName("ReloadAsync 應重新載入設定")]
    public async Task ReloadAsync_ShouldRefreshCache()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDbContext<MithyaDbContext>(options =>
            options.UseInMemoryDatabase("ProxyCacheTest_Reload"));
        services.AddScoped<Core.Interfaces.IProxyConfigRepository,
            Infrastructure.Repositories.ProxyConfigRepository>();

        var sp = services.BuildServiceProvider();

        using (var scope = sp.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MithyaDbContext>();
            db.Database.EnsureCreated();
        }

        var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();
        var cache = new ProxyConfigCache(scopeFactory);

        await cache.LoadAllAsync();
        cache.GetGlobalActive().Should().BeNull();

        // Add a config after initial load
        using (var scope = sp.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MithyaDbContext>();
            db.ProxyConfigs.Add(new ProxyConfig
            {
                Id = Guid.NewGuid(),
                TargetBaseUrl = "https://new.example.com",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        // Act
        await cache.ReloadAsync();

        // Assert
        cache.GetGlobalActive().Should().NotBeNull();
        cache.GetGlobalActive()!.TargetBaseUrl.Should().Be("https://new.example.com");
    }
}
