using System.ComponentModel;
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MockServer.Api.DTOs.Requests;
using MockServer.Core.Entities;
using MockServer.Core.Enums;
using MockServer.Infrastructure.Data;
using Xunit;

namespace MockServer.Tests.Integration;

public class EndpointApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public EndpointApiTests(WebApplicationFactory<Program> factory)
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
                    options.UseInMemoryDatabase("TestDb");
                });

                // Build service provider and create database
                var sp = services.BuildServiceProvider();
                using var scope = sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<MockServerDbContext>();
                db.Database.EnsureCreated();
            });

            // Use test server without WireMock
            builder.UseSetting("WireMock:Port", "0"); // Disable WireMock
        });
    }

    [Fact]
    [DisplayName("取得協議列表 API 應該返回所有協議 Schema")]
    public async Task GetProtocols_ShouldReturnProtocolSchemas()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/admin/api/protocols");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var schemas = await response.Content.ReadFromJsonAsync<List<MockServer.Core.ValueObjects.ProtocolSchema>>();
        schemas.Should().NotBeNull();
        schemas.Should().HaveCountGreaterThan(0);
        schemas.Should().Contain(s => s.Protocol == ProtocolType.REST);
        schemas.Should().Contain(s => s.Protocol == ProtocolType.SOAP);
    }

    [Fact]
    [DisplayName("建立 Endpoint API 使用有效請求應該返回 201")]
    public async Task CreateEndpoint_ValidRequest_ShouldReturn201()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new CreateEndpointRequest
        {
            Name = "Test Endpoint",
            ServiceName = "Test Service",
            Protocol = ProtocolType.REST,
            Path = "/api/test",
            HttpMethod = "POST"
        };

        // Act
        var response = await client.PostAsJsonAsync("/admin/api/endpoints", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var endpoint = await response.Content.ReadFromJsonAsync<MockEndpoint>();
        endpoint.Should().NotBeNull();
        endpoint!.Name.Should().Be("Test Endpoint");
        endpoint.Path.Should().Be("/api/test");
    }

    [Fact]
    [DisplayName("建立 Endpoint API 使用無效 Path 應該返回 400")]
    public async Task CreateEndpoint_InvalidPath_ShouldReturn400()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new CreateEndpointRequest
        {
            Name = "Test Endpoint",
            ServiceName = "Test Service",
            Protocol = ProtocolType.REST,
            Path = "", // Invalid
            HttpMethod = "POST"
        };

        // Act
        var response = await client.PostAsJsonAsync("/admin/api/endpoints", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    [DisplayName("取得所有 Endpoint API 應該返回 Endpoint 列表")]
    public async Task GetAllEndpoints_ShouldReturnEndpoints()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Create some endpoints first
        var request1 = new CreateEndpointRequest
        {
            Name = "Endpoint 1",
            ServiceName = "Service 1",
            Protocol = ProtocolType.REST,
            Path = "/api/endpoint1",
            HttpMethod = "GET"
        };

        var request2 = new CreateEndpointRequest
        {
            Name = "Endpoint 2",
            ServiceName = "Service 2",
            Protocol = ProtocolType.SOAP,
            Path = "/soap/endpoint2",
            HttpMethod = "POST"
        };

        await client.PostAsJsonAsync("/admin/api/endpoints", request1);
        await client.PostAsJsonAsync("/admin/api/endpoints", request2);

        // Act
        var response = await client.GetAsync("/admin/api/endpoints");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var endpoints = await response.Content.ReadFromJsonAsync<List<MockEndpoint>>();
        endpoints.Should().NotBeNull();
        endpoints.Should().HaveCountGreaterOrEqualTo(2);
    }
}
