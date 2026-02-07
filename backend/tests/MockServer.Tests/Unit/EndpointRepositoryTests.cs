using System.ComponentModel;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MockServer.Core.Entities;
using MockServer.Core.Enums;
using MockServer.Infrastructure.Data;
using MockServer.Infrastructure.Repositories;
using Xunit;

namespace MockServer.Tests.Unit;

public class EndpointRepositoryTests
{
    private MockServerDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<MockServerDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new MockServerDbContext(options);
    }

    [Fact]
    [DisplayName("新增 Endpoint 應該成功")]
    public async Task AddAsync_ShouldAddEndpoint()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new EndpointRepository(context);
        var endpoint = new MockEndpoint
        {
            Name = "Test Endpoint",
            ServiceName = "Test Service",
            Protocol = ProtocolType.REST,
            Path = "/api/test",
            HttpMethod = "POST",
            IsActive = true
        };

        // Act
        await repository.AddAsync(endpoint);
        await repository.SaveChangesAsync();

        // Assert
        endpoint.Id.Should().NotBe(Guid.Empty);
        endpoint.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    [DisplayName("根據 ID 取得已存在的 Endpoint 應該返回資料")]
    public async Task GetByIdAsync_ExistingEndpoint_ShouldReturnEndpoint()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new EndpointRepository(context);
        var endpoint = new MockEndpoint
        {
            Name = "Test Endpoint",
            ServiceName = "Test Service",
            Protocol = ProtocolType.REST,
            Path = "/api/test",
            HttpMethod = "POST",
            IsActive = true
        };

        await repository.AddAsync(endpoint);
        await repository.SaveChangesAsync();

        // Act
        var result = await repository.GetByIdAsync(endpoint.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Test Endpoint");
        result.Path.Should().Be("/api/test");
    }

    [Fact]
    [DisplayName("取得所有啟用的 Endpoint 應該只返回 IsActive 為 true 的資料")]
    public async Task GetAllActiveAsync_ShouldReturnOnlyActiveEndpoints()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new EndpointRepository(context);

        var activeEndpoint = new MockEndpoint
        {
            Name = "Active",
            ServiceName = "Service",
            Protocol = ProtocolType.REST,
            Path = "/api/active",
            HttpMethod = "POST",
            IsActive = true
        };

        var inactiveEndpoint = new MockEndpoint
        {
            Name = "Inactive",
            ServiceName = "Service",
            Protocol = ProtocolType.REST,
            Path = "/api/inactive",
            HttpMethod = "POST",
            IsActive = false
        };

        await repository.AddAsync(activeEndpoint);
        await repository.AddAsync(inactiveEndpoint);
        await repository.SaveChangesAsync();

        // Act
        var result = await repository.GetAllActiveAsync();

        // Assert
        result.Should().HaveCount(1);
        result.First().Name.Should().Be("Active");
    }

    [Fact]
    [DisplayName("更新 Endpoint 應該成功並更新 UpdatedAt")]
    public async Task UpdateAsync_ShouldUpdateEndpoint()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new EndpointRepository(context);
        var endpoint = new MockEndpoint
        {
            Name = "Original",
            ServiceName = "Service",
            Protocol = ProtocolType.REST,
            Path = "/api/test",
            HttpMethod = "POST",
            IsActive = true
        };

        await repository.AddAsync(endpoint);
        await repository.SaveChangesAsync();

        // Act
        endpoint.Name = "Updated";
        await repository.UpdateAsync(endpoint);
        await repository.SaveChangesAsync();

        // Assert
        var updated = await repository.GetByIdAsync(endpoint.Id);
        updated!.Name.Should().Be("Updated");
        updated.UpdatedAt.Should().BeAfter(updated.CreatedAt);
    }

    [Fact]
    [DisplayName("刪除 Endpoint 應該成功")]
    public async Task DeleteAsync_ShouldRemoveEndpoint()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new EndpointRepository(context);
        var endpoint = new MockEndpoint
        {
            Name = "Test",
            ServiceName = "Service",
            Protocol = ProtocolType.REST,
            Path = "/api/test",
            HttpMethod = "POST",
            IsActive = true
        };

        await repository.AddAsync(endpoint);
        await repository.SaveChangesAsync();

        // Act
        await repository.DeleteAsync(endpoint.Id);
        await repository.SaveChangesAsync();

        // Assert
        var result = await repository.GetByIdAsync(endpoint.Id);
        result.Should().BeNull();
    }
}
