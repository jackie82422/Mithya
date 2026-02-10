using System.ComponentModel;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MockServer.Core.Entities;
using MockServer.Core.Enums;
using MockServer.Infrastructure.Data;
using MockServer.Infrastructure.Repositories;
using Xunit;

namespace MockServer.Tests.Unit;

public class RuleRepositoryTests
{
    private MockServerDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<MockServerDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new MockServerDbContext(options);
    }

    private async Task<MockEndpoint> CreateTestEndpoint(MockServerDbContext context)
    {
        var repo = new EndpointRepository(context);
        var endpoint = new MockEndpoint
        {
            Name = "Test Endpoint",
            ServiceName = "TestService",
            Protocol = ProtocolType.REST,
            Path = "/api/test",
            HttpMethod = "POST",
            IsActive = true
        };
        await repo.AddAsync(endpoint);
        await repo.SaveChangesAsync();
        return endpoint;
    }

    [Fact]
    [DisplayName("新增 Rule 應該成功")]
    public async Task AddAsync_ShouldAddRule()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var endpoint = await CreateTestEndpoint(context);
        var repository = new RuleRepository(context);
        var rule = new MockRule
        {
            EndpointId = endpoint.Id,
            RuleName = "Test Rule",
            Priority = 1,
            MatchConditions = "[]",
            ResponseStatusCode = 200,
            ResponseBody = "{\"ok\": true}",
            IsActive = true
        };

        // Act
        await repository.AddAsync(rule);
        await repository.SaveChangesAsync();

        // Assert
        rule.Id.Should().NotBe(Guid.Empty);
        rule.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    [DisplayName("根據 ID 取得 Rule 應返回資料")]
    public async Task GetByIdAsync_ShouldReturnRule()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var endpoint = await CreateTestEndpoint(context);
        var repository = new RuleRepository(context);
        var rule = new MockRule
        {
            EndpointId = endpoint.Id,
            RuleName = "Get By ID Rule",
            Priority = 1,
            MatchConditions = "[]",
            ResponseStatusCode = 200,
            ResponseBody = "{}",
            IsActive = true
        };
        await repository.AddAsync(rule);
        await repository.SaveChangesAsync();

        // Act
        var result = await repository.GetByIdAsync(rule.Id);

        // Assert
        result.Should().NotBeNull();
        result!.RuleName.Should().Be("Get By ID Rule");
    }

    [Fact]
    [DisplayName("根據 EndpointId 取得 Rules 應按 Priority 排序")]
    public async Task GetByEndpointIdAsync_ShouldReturnOrderedByPriority()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var endpoint = await CreateTestEndpoint(context);
        var repository = new RuleRepository(context);

        await repository.AddAsync(new MockRule
        {
            EndpointId = endpoint.Id,
            RuleName = "Low Priority",
            Priority = 10,
            MatchConditions = "[]",
            ResponseStatusCode = 200,
            ResponseBody = "{}",
            IsActive = true
        });
        await repository.AddAsync(new MockRule
        {
            EndpointId = endpoint.Id,
            RuleName = "High Priority",
            Priority = 1,
            MatchConditions = "[]",
            ResponseStatusCode = 200,
            ResponseBody = "{}",
            IsActive = true
        });
        await repository.SaveChangesAsync();

        // Act
        var rules = (await repository.GetByEndpointIdAsync(endpoint.Id)).ToList();

        // Assert
        rules.Should().HaveCount(2);
        rules[0].RuleName.Should().Be("High Priority");
        rules[1].RuleName.Should().Be("Low Priority");
    }

    [Fact]
    [DisplayName("更新 Rule 應成功並更新 UpdatedAt")]
    public async Task UpdateAsync_ShouldUpdateRule()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var endpoint = await CreateTestEndpoint(context);
        var repository = new RuleRepository(context);
        var rule = new MockRule
        {
            EndpointId = endpoint.Id,
            RuleName = "Original",
            Priority = 1,
            MatchConditions = "[]",
            ResponseStatusCode = 200,
            ResponseBody = "{}",
            IsActive = true
        };
        await repository.AddAsync(rule);
        await repository.SaveChangesAsync();

        // Act
        rule.RuleName = "Updated";
        await repository.UpdateAsync(rule);
        await repository.SaveChangesAsync();

        // Assert
        var updated = await repository.GetByIdAsync(rule.Id);
        updated!.RuleName.Should().Be("Updated");
        updated.UpdatedAt.Should().BeAfter(updated.CreatedAt);
    }

    [Fact]
    [DisplayName("刪除 Rule 應成功")]
    public async Task DeleteAsync_ShouldRemoveRule()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var endpoint = await CreateTestEndpoint(context);
        var repository = new RuleRepository(context);
        var rule = new MockRule
        {
            EndpointId = endpoint.Id,
            RuleName = "To Delete",
            Priority = 1,
            MatchConditions = "[]",
            ResponseStatusCode = 200,
            ResponseBody = "{}",
            IsActive = true
        };
        await repository.AddAsync(rule);
        await repository.SaveChangesAsync();

        // Act
        await repository.DeleteAsync(rule.Id);
        await repository.SaveChangesAsync();

        // Assert
        var result = await repository.GetByIdAsync(rule.Id);
        result.Should().BeNull();
    }
}
