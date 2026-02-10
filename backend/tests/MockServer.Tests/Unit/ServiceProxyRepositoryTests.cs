using System.ComponentModel;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MockServer.Core.Entities;
using MockServer.Infrastructure.Data;
using MockServer.Infrastructure.Repositories;
using Xunit;

namespace MockServer.Tests.Unit;

public class ServiceProxyRepositoryTests
{
    private MockServerDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<MockServerDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new MockServerDbContext(options);
    }

    [Fact]
    [DisplayName("新增 ServiceProxy 應該成功")]
    public async Task AddAsync_ShouldAddProxy()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new ServiceProxyRepository(context);
        var proxy = new ServiceProxy
        {
            ServiceName = "UserService",
            TargetBaseUrl = "https://api.example.com",
            IsActive = true
        };

        // Act
        await repository.AddAsync(proxy);
        await repository.SaveChangesAsync();

        // Assert
        proxy.Id.Should().NotBe(Guid.Empty);
        proxy.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    [DisplayName("根據 ID 取得 ServiceProxy 應返回資料")]
    public async Task GetByIdAsync_ShouldReturnProxy()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new ServiceProxyRepository(context);
        var proxy = new ServiceProxy
        {
            ServiceName = "TestService",
            TargetBaseUrl = "https://test.example.com",
            IsActive = true
        };
        await repository.AddAsync(proxy);
        await repository.SaveChangesAsync();

        // Act
        var result = await repository.GetByIdAsync(proxy.Id);

        // Assert
        result.Should().NotBeNull();
        result!.ServiceName.Should().Be("TestService");
    }

    [Fact]
    [DisplayName("根據 ServiceName 取得 ServiceProxy")]
    public async Task GetByServiceNameAsync_ShouldReturnProxy()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new ServiceProxyRepository(context);
        var proxy = new ServiceProxy
        {
            ServiceName = "NameLookup",
            TargetBaseUrl = "https://name.example.com",
            IsActive = true
        };
        await repository.AddAsync(proxy);
        await repository.SaveChangesAsync();

        // Act
        var result = await repository.GetByServiceNameAsync("NameLookup");

        // Assert
        result.Should().NotBeNull();
        result!.TargetBaseUrl.Should().Be("https://name.example.com");
    }

    [Fact]
    [DisplayName("不存在的 ServiceName 應返回 null")]
    public async Task GetByServiceNameAsync_NotFound_ShouldReturnNull()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new ServiceProxyRepository(context);

        // Act
        var result = await repository.GetByServiceNameAsync("NonExistent");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    [DisplayName("取得所有 ServiceProxy 應按建立時間倒序排列")]
    public async Task GetAllAsync_ShouldReturnOrderedByCreatedAtDesc()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new ServiceProxyRepository(context);

        await repository.AddAsync(new ServiceProxy
        {
            ServiceName = "First",
            TargetBaseUrl = "https://first.example.com",
            IsActive = true
        });
        await repository.SaveChangesAsync();

        // Small delay to ensure different timestamps
        await Task.Delay(10);

        await repository.AddAsync(new ServiceProxy
        {
            ServiceName = "Second",
            TargetBaseUrl = "https://second.example.com",
            IsActive = true
        });
        await repository.SaveChangesAsync();

        // Act
        var proxies = (await repository.GetAllAsync()).ToList();

        // Assert
        proxies.Should().HaveCount(2);
        proxies[0].ServiceName.Should().Be("Second");
        proxies[1].ServiceName.Should().Be("First");
    }

    [Fact]
    [DisplayName("更新 ServiceProxy 應成功並更新 UpdatedAt")]
    public async Task UpdateAsync_ShouldUpdateProxy()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new ServiceProxyRepository(context);
        var proxy = new ServiceProxy
        {
            ServiceName = "Original",
            TargetBaseUrl = "https://original.example.com",
            IsActive = true
        };
        await repository.AddAsync(proxy);
        await repository.SaveChangesAsync();

        // Act
        proxy.TargetBaseUrl = "https://updated.example.com";
        await repository.UpdateAsync(proxy);
        await repository.SaveChangesAsync();

        // Assert
        var updated = await repository.GetByIdAsync(proxy.Id);
        updated!.TargetBaseUrl.Should().Be("https://updated.example.com");
        updated.UpdatedAt.Should().BeAfter(updated.CreatedAt);
    }

    [Fact]
    [DisplayName("刪除 ServiceProxy 應成功")]
    public async Task DeleteAsync_ShouldRemoveProxy()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new ServiceProxyRepository(context);
        var proxy = new ServiceProxy
        {
            ServiceName = "ToDelete",
            TargetBaseUrl = "https://delete.example.com",
            IsActive = true
        };
        await repository.AddAsync(proxy);
        await repository.SaveChangesAsync();

        // Act
        await repository.DeleteAsync(proxy.Id);
        await repository.SaveChangesAsync();

        // Assert
        var result = await repository.GetByIdAsync(proxy.Id);
        result.Should().BeNull();
    }

    [Fact]
    [DisplayName("刪除不存在的 ID 不應拋出例外")]
    public async Task DeleteAsync_NonExistent_ShouldNotThrow()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new ServiceProxyRepository(context);

        // Act & Assert
        await FluentActions.Invoking(async () =>
        {
            await repository.DeleteAsync(Guid.NewGuid());
            await repository.SaveChangesAsync();
        }).Should().NotThrowAsync();
    }
}
