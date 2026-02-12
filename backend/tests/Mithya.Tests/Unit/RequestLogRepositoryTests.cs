using System.ComponentModel;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Mithya.Core.Entities;
using Mithya.Infrastructure.Data;
using Mithya.Infrastructure.Repositories;
using Xunit;

namespace Mithya.Tests.Unit;

public class RequestLogRepositoryTests
{
    private MithyaDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<MithyaDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new MithyaDbContext(options);
    }

    [Fact]
    [DisplayName("新增 Log 應該成功")]
    public async Task AddAsync_ShouldAddLog()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new RequestLogRepository(context);
        var log = new MockRequestLog
        {
            Method = "GET",
            Path = "/api/test",
            ResponseStatusCode = 200,
            IsMatched = true
        };

        // Act
        await repository.AddAsync(log);
        await repository.SaveChangesAsync();

        // Assert
        log.Id.Should().NotBe(Guid.Empty);
        log.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    [DisplayName("取得 Logs 應按時間倒序排列")]
    public async Task GetLogsAsync_ShouldReturnOrderedByTimestampDesc()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new RequestLogRepository(context);

        await repository.AddAsync(new MockRequestLog
        {
            Method = "GET",
            Path = "/api/first",
            ResponseStatusCode = 200,
            IsMatched = true
        });
        await repository.SaveChangesAsync();

        await Task.Delay(10);

        await repository.AddAsync(new MockRequestLog
        {
            Method = "POST",
            Path = "/api/second",
            ResponseStatusCode = 201,
            IsMatched = true
        });
        await repository.SaveChangesAsync();

        // Act
        var logs = (await repository.GetLogsAsync()).ToList();

        // Assert
        logs.Should().HaveCount(2);
        logs[0].Path.Should().Be("/api/second");
        logs[1].Path.Should().Be("/api/first");
    }

    [Fact]
    [DisplayName("取得 Logs 應受 limit 限制")]
    public async Task GetLogsAsync_ShouldRespectLimit()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new RequestLogRepository(context);

        for (int i = 0; i < 5; i++)
        {
            await repository.AddAsync(new MockRequestLog
            {
                Method = "GET",
                Path = $"/api/test{i}",
                ResponseStatusCode = 200,
                IsMatched = true
            });
        }
        await repository.SaveChangesAsync();

        // Act
        var logs = (await repository.GetLogsAsync(limit: 3)).ToList();

        // Assert
        logs.Should().HaveCount(3);
    }

    [Fact]
    [DisplayName("根據 EndpointId 取得 Logs")]
    public async Task GetLogsByEndpointAsync_ShouldFilterByEndpoint()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new RequestLogRepository(context);
        var endpointId = Guid.NewGuid();

        await repository.AddAsync(new MockRequestLog
        {
            Method = "GET",
            Path = "/api/mine",
            ResponseStatusCode = 200,
            EndpointId = endpointId,
            IsMatched = true
        });
        await repository.AddAsync(new MockRequestLog
        {
            Method = "GET",
            Path = "/api/other",
            ResponseStatusCode = 200,
            EndpointId = Guid.NewGuid(),
            IsMatched = true
        });
        await repository.SaveChangesAsync();

        // Act
        var logs = (await repository.GetLogsByEndpointAsync(endpointId)).ToList();

        // Assert
        logs.Should().HaveCount(1);
        logs[0].Path.Should().Be("/api/mine");
    }

    [Fact]
    [DisplayName("刪除所有 Logs 應清空")]
    public async Task DeleteAllAsync_ShouldRemoveAllLogs()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new RequestLogRepository(context);

        await repository.AddAsync(new MockRequestLog
        {
            Method = "GET",
            Path = "/api/test1",
            ResponseStatusCode = 200,
            IsMatched = true
        });
        await repository.AddAsync(new MockRequestLog
        {
            Method = "POST",
            Path = "/api/test2",
            ResponseStatusCode = 201,
            IsMatched = true
        });
        await repository.SaveChangesAsync();

        // Act
        await repository.DeleteAllAsync();

        // Assert
        var logs = (await repository.GetLogsAsync()).ToList();
        logs.Should().BeEmpty();
    }
}
