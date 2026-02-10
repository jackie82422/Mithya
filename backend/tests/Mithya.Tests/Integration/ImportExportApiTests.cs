using System.ComponentModel;
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mithya.Core.Entities;
using Mithya.Infrastructure.Data;
using Xunit;

namespace Mithya.Tests.Integration;

public class ImportExportApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ImportExportApiTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<MithyaDbContext>));
                if (descriptor != null)
                    services.Remove(descriptor);

                services.AddDbContext<MithyaDbContext>(options =>
                    options.UseInMemoryDatabase("TestDb_ImportExport"));

                var sp = services.BuildServiceProvider();
                using var scope = sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<MithyaDbContext>();
                db.Database.EnsureCreated();
            });
        });
    }

    [Fact]
    [DisplayName("GET /admin/api/export 應返回所有 endpoint 及 rule")]
    public async Task Export_ShouldReturnAllEndpointsWithRules()
    {
        // Arrange
        var client = _factory.CreateClient();

        var endpointResponse = await client.PostAsJsonAsync("/admin/api/endpoints", new
        {
            name = "Export Test",
            serviceName = "Test",
            protocol = 1,
            path = "/api/export-test",
            httpMethod = "GET"
        });
        endpointResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var endpoint = await endpointResponse.Content.ReadFromJsonAsync<MockEndpoint>();
        var endpointId = endpoint!.Id.ToString();

        await client.PostAsJsonAsync($"/admin/api/endpoints/{endpointId}/rules", new
        {
            ruleName = "Export Rule",
            priority = 1,
            conditions = Array.Empty<object>(),
            statusCode = 200,
            responseBody = "{\"ok\":true}"
        });

        // Act
        var response = await client.GetAsync("/admin/api/export");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Export Test");
        body.Should().Contain("Export Rule");
        body.Should().Contain("version");
    }

    [Fact]
    [DisplayName("POST /admin/api/import/json 應匯入 endpoint 及 rule")]
    public async Task ImportJson_ShouldCreateEndpointsAndRules()
    {
        // Arrange
        var client = _factory.CreateClient();

        var importData = new
        {
            endpoints = new[]
            {
                new
                {
                    name = "Imported Endpoint",
                    serviceName = "ImportTest",
                    protocol = 1,
                    path = "/api/imported",
                    httpMethod = "POST",
                    isActive = true,
                    rules = new[]
                    {
                        new
                        {
                            ruleName = "Imported Rule",
                            priority = 10,
                            matchConditions = "[]",
                            responseStatusCode = 201,
                            responseBody = "{\"imported\":true}",
                            delayMs = 0,
                            isTemplate = false,
                            isResponseHeadersTemplate = false,
                            faultType = 0,
                            logicMode = 0,
                            isActive = true
                        }
                    }
                }
            }
        };

        // Act
        var response = await client.PostAsJsonAsync("/admin/api/import/json", importData);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Imported Endpoint");
        body.Should().Contain("\"imported\":1");
    }

    [Fact]
    [DisplayName("POST /admin/api/import/openapi 應從 OpenAPI 規格匯入 endpoint")]
    public async Task ImportOpenApi_ShouldCreateEndpointsFromSpec()
    {
        // Arrange
        var client = _factory.CreateClient();
        var openApiSpec = """
        {
            "openapi": "3.0.0",
            "info": { "title": "Pet Store", "version": "1.0" },
            "paths": {
                "/pets": {
                    "get": {
                        "summary": "List pets",
                        "responses": {
                            "200": {
                                "description": "OK",
                                "content": {
                                    "application/json": {
                                        "example": [{"name":"Fido"}]
                                    }
                                }
                            }
                        }
                    },
                    "post": {
                        "summary": "Create pet",
                        "responses": { "201": { "description": "Created" } }
                    }
                }
            }
        }
        """;

        // Act
        var response = await client.PostAsJsonAsync("/admin/api/import/openapi", new { spec = openApiSpec });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("List pets");
        body.Should().Contain("Create pet");
        body.Should().Contain("\"imported\":2");
    }

    [Fact]
    [DisplayName("POST /admin/api/import/json 無 endpoint 應返回 400")]
    public async Task ImportJson_EmptyEndpoints_ShouldReturn400()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/admin/api/import/json", new { endpoints = Array.Empty<object>() });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    [DisplayName("POST /admin/api/import/openapi 無效規格應返回 400")]
    public async Task ImportOpenApi_InvalidSpec_ShouldReturn400()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/admin/api/import/openapi", new { spec = "not json" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    [DisplayName("匯入重複 path+method 應跳過並回傳 duplicates")]
    public async Task ImportJson_DuplicateEndpoint_ShouldSkipAndReport()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Create an existing endpoint
        await client.PostAsJsonAsync("/admin/api/endpoints", new
        {
            name = "Existing EP",
            serviceName = "Test",
            protocol = 1,
            path = "/api/dup-test",
            httpMethod = "GET"
        });

        // Act - import with same path+method
        var response = await client.PostAsJsonAsync("/admin/api/import/json", new
        {
            endpoints = new[]
            {
                new
                {
                    name = "Duplicate EP",
                    serviceName = "Test",
                    protocol = 1,
                    path = "/api/dup-test",
                    httpMethod = "GET",
                    isActive = true
                },
                new
                {
                    name = "New EP",
                    serviceName = "Test",
                    protocol = 1,
                    path = "/api/new-import",
                    httpMethod = "POST",
                    isActive = true
                }
            }
        });

        // Assert - 200, not 500; imported=1, skipped=1
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("\"imported\":1");
        body.Should().Contain("\"skipped\":1");
        body.Should().Contain("duplicate");
    }

    [Fact]
    [DisplayName("匯出再匯入應保留所有資料")]
    public async Task ExportThenImport_ShouldPreserveData()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Create an endpoint
        var createResp = await client.PostAsJsonAsync("/admin/api/endpoints", new
        {
            name = "Roundtrip Test",
            serviceName = "RoundTrip",
            protocol = 1,
            path = "/api/roundtrip",
            httpMethod = "PUT"
        });
        createResp.StatusCode.Should().Be(HttpStatusCode.Created);

        // Export
        var exportResp = await client.GetAsync("/admin/api/export");
        exportResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var exportBody = await exportResp.Content.ReadAsStringAsync();

        // Assert export contains the endpoint
        exportBody.Should().Contain("Roundtrip Test");
        exportBody.Should().Contain("/api/roundtrip");
    }
}
