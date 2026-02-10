using System.ComponentModel;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MockServer.Core.Entities;
using MockServer.Infrastructure.Data;
using MockServer.Infrastructure.MockEngine;
using Xunit;

namespace MockServer.Tests.Unit;

public class ServiceProxyCacheTests
{
    private IServiceScopeFactory BuildScopeFactory(string dbName, params ServiceProxy[] proxies)
    {
        var services = new ServiceCollection();
        services.AddDbContext<MockServerDbContext>(options =>
            options.UseInMemoryDatabase(dbName));

        services.AddScoped<Core.Interfaces.IServiceProxyRepository,
            Infrastructure.Repositories.ServiceProxyRepository>();

        var sp = services.BuildServiceProvider();

        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MockServerDbContext>();
        db.Database.EnsureCreated();
        foreach (var p in proxies)
        {
            p.Id = Guid.NewGuid();
            p.CreatedAt = DateTime.UtcNow;
            p.UpdatedAt = DateTime.UtcNow;
            db.ServiceProxies.Add(p);
        }
        db.SaveChanges();

        return sp.GetRequiredService<IServiceScopeFactory>();
    }

    [Fact]
    [DisplayName("LoadAllAsync 應載入啟用的 Service Proxy")]
    public async Task LoadAll_ShouldLoadActiveProxy()
    {
        // Arrange
        var factory = BuildScopeFactory("SPCacheTest_Active", new ServiceProxy
        {
            ServiceName = "UserService",
            TargetBaseUrl = "https://api.example.com",
            IsActive = true
        });
        var cache = new ServiceProxyCache(factory);

        // Act
        await cache.LoadAllAsync();

        // Assert
        cache.GetActiveByServiceName("UserService").Should().NotBeNull();
        cache.GetActiveByServiceName("UserService")!.TargetBaseUrl.Should().Be("https://api.example.com");
    }

    [Fact]
    [DisplayName("LoadAllAsync 不應載入非啟用的 Proxy")]
    public async Task LoadAll_ShouldSkipInactiveProxy()
    {
        // Arrange
        var factory = BuildScopeFactory("SPCacheTest_Inactive", new ServiceProxy
        {
            ServiceName = "InactiveService",
            TargetBaseUrl = "https://inactive.example.com",
            IsActive = false
        });
        var cache = new ServiceProxyCache(factory);

        // Act
        await cache.LoadAllAsync();

        // Assert
        cache.GetActiveByServiceName("InactiveService").Should().BeNull();
        cache.GetAllActive().Should().BeEmpty();
    }

    [Fact]
    [DisplayName("GetActiveByServiceName 應支援大小寫不敏感查詢")]
    public async Task GetActiveByServiceName_ShouldBeCaseInsensitive()
    {
        // Arrange
        var factory = BuildScopeFactory("SPCacheTest_Case", new ServiceProxy
        {
            ServiceName = "MyService",
            TargetBaseUrl = "https://case.example.com",
            IsActive = true
        });
        var cache = new ServiceProxyCache(factory);
        await cache.LoadAllAsync();

        // Act & Assert
        cache.GetActiveByServiceName("myservice").Should().NotBeNull();
        cache.GetActiveByServiceName("MYSERVICE").Should().NotBeNull();
        cache.GetActiveByServiceName("MyService").Should().NotBeNull();
    }

    [Fact]
    [DisplayName("GetAllActive 應返回所有啟用的 Proxy")]
    public async Task GetAllActive_ShouldReturnAllActive()
    {
        // Arrange
        var factory = BuildScopeFactory("SPCacheTest_AllActive",
            new ServiceProxy { ServiceName = "Svc1", TargetBaseUrl = "https://svc1.example.com", IsActive = true },
            new ServiceProxy { ServiceName = "Svc2", TargetBaseUrl = "https://svc2.example.com", IsActive = true },
            new ServiceProxy { ServiceName = "Svc3", TargetBaseUrl = "https://svc3.example.com", IsActive = false }
        );
        var cache = new ServiceProxyCache(factory);

        // Act
        await cache.LoadAllAsync();

        // Assert
        cache.GetAllActive().Should().HaveCount(2);
    }

    [Fact]
    [DisplayName("ReloadAsync 應重新載入設定")]
    public async Task ReloadAsync_ShouldRefreshCache()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDbContext<MockServerDbContext>(options =>
            options.UseInMemoryDatabase("SPCacheTest_Reload"));
        services.AddScoped<Core.Interfaces.IServiceProxyRepository,
            Infrastructure.Repositories.ServiceProxyRepository>();

        var sp = services.BuildServiceProvider();

        using (var scope = sp.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MockServerDbContext>();
            db.Database.EnsureCreated();
        }

        var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();
        var cache = new ServiceProxyCache(scopeFactory);

        await cache.LoadAllAsync();
        cache.GetAllActive().Should().BeEmpty();

        // Add a proxy after initial load
        using (var scope = sp.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MockServerDbContext>();
            db.ServiceProxies.Add(new ServiceProxy
            {
                Id = Guid.NewGuid(),
                ServiceName = "NewService",
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
        cache.GetActiveByServiceName("NewService").Should().NotBeNull();
        cache.GetAllActive().Should().HaveCount(1);
    }

    [Fact]
    [DisplayName("不存在的 ServiceName 應返回 null")]
    public async Task GetActiveByServiceName_NotFound_ShouldReturnNull()
    {
        // Arrange
        var factory = BuildScopeFactory("SPCacheTest_NotFound");
        var cache = new ServiceProxyCache(factory);
        await cache.LoadAllAsync();

        // Act & Assert
        cache.GetActiveByServiceName("NonExistent").Should().BeNull();
    }
}
