using System.ComponentModel;
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MockServer.Core.Entities;
using MockServer.Infrastructure.Data;
using Newtonsoft.Json;
using Xunit;

namespace MockServer.Tests.Integration;

public class LogApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly string _testDbName;

    public LogApiTests(WebApplicationFactory<Program> factory)
    {
        _testDbName = $"TestDb_Logs_{Guid.NewGuid()}";

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
                    options.UseInMemoryDatabase(_testDbName);
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
    [DisplayName("請求日誌 API 應該返回空列表當沒有請求時")]
    public async Task GetLogs_NoRequests_ShouldReturnEmptyList()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/admin/api/logs");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var logs = await response.Content.ReadFromJsonAsync<List<MockRequestLog>>();
        logs.Should().NotBeNull();
        logs.Should().BeEmpty();
    }

    [Fact]
    [DisplayName("請求日誌 API 應該記錄 Mock Server 收到的請求")]
    public async Task GetLogs_AfterMockRequest_ShouldReturnLoggedRequest()
    {
        // Arrange - Use the same test server client (single port architecture)
        var testDbName = $"TestDb_Logs_Mock_{Guid.NewGuid()}";

        var factoryWithMock = _factory.WithWebHostBuilder(builder =>
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

                // Add InMemory database
                services.AddDbContext<MockServerDbContext>(options =>
                {
                    options.UseInMemoryDatabase(testDbName);
                });

                var sp = services.BuildServiceProvider();
                using var scope = sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<MockServerDbContext>();
                db.Database.EnsureCreated();
            });
        });

        var client = factoryWithMock.CreateClient();

        // Create endpoint via admin API
        var createEndpointResponse = await client.PostAsJsonAsync("/admin/api/endpoints", new
        {
            name = "測試端點",
            serviceName = "測試服務",
            protocol = 1,
            path = "/api/test",
            httpMethod = "POST"
        });

        var endpoint = await createEndpointResponse.Content.ReadFromJsonAsync<MockEndpoint>();
        endpoint.Should().NotBeNull();

        // Create rule via admin API
        await client.PostAsJsonAsync($"/admin/api/endpoints/{endpoint!.Id}/rules", new
        {
            ruleName = "測試規則",
            priority = 1,
            conditions = new[]
            {
                new
                {
                    sourceType = 1, // Body
                    fieldPath = "$.userId",
                    @operator = 1,  // Equals
                    value = "12345"
                }
            },
            statusCode = 200,
            responseBody = "{\"status\":\"success\"}",
            delayMs = 0
        });

        // Act - Send mock request through the same test server (single port)
        var mockResponse = await client.PostAsync("/api/test", JsonContent.Create(new
        {
            userId = "12345"
        }));

        mockResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Get logs
        var logsResponse = await client.GetAsync("/admin/api/logs");
        logsResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var logs = await logsResponse.Content.ReadFromJsonAsync<List<MockRequestLog>>();

        // Assert
        logs.Should().NotBeNull();
        logs.Should().HaveCountGreaterThan(0);

        var log = logs!.First();
        log.Method.Should().Be("POST");
        log.Path.Should().Be("/api/test");
        log.ResponseStatusCode.Should().Be(200);
        log.EndpointId.Should().Be(endpoint.Id);
        log.IsMatched.Should().BeTrue();
        log.ResponseTimeMs.Should().BeGreaterOrEqualTo(0);
    }

    [Fact]
    [DisplayName("請求日誌 API 應該按端點 ID 過濾日誌")]
    public async Task GetLogsByEndpoint_ValidEndpointId_ShouldReturnFilteredLogs()
    {
        // Arrange
        var client = _factory.CreateClient();
        var testEndpointId = Guid.NewGuid();

        // Manually insert test log
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MockServerDbContext>();

            db.MockRequestLogs.Add(new MockRequestLog
            {
                Id = Guid.NewGuid(),
                EndpointId = testEndpointId,
                Method = "GET",
                Path = "/api/test",
                ResponseStatusCode = 200,
                IsMatched = true,
                Timestamp = DateTime.UtcNow
            });

            db.MockRequestLogs.Add(new MockRequestLog
            {
                Id = Guid.NewGuid(),
                EndpointId = Guid.NewGuid(), // Different endpoint
                Method = "POST",
                Path = "/api/other",
                ResponseStatusCode = 404,
                IsMatched = false,
                Timestamp = DateTime.UtcNow
            });

            await db.SaveChangesAsync();
        }

        // Act
        var response = await client.GetAsync($"/admin/api/logs/endpoint/{testEndpointId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var logs = await response.Content.ReadFromJsonAsync<List<MockRequestLog>>();
        logs.Should().NotBeNull();
        logs.Should().HaveCount(1);
        logs!.First().EndpointId.Should().Be(testEndpointId);
    }
}
