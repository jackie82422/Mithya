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

public class TryRequestApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public TryRequestApiTests(WebApplicationFactory<Program> factory)
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
                    options.UseInMemoryDatabase("TestDb_TryRequest"));

                var sp = services.BuildServiceProvider();
                using var scope = sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<MithyaDbContext>();
                db.Database.EnsureCreated();
            });
        });
    }

    [Fact]
    [DisplayName("缺少 method 應返回 400")]
    public async Task TryRequest_MissingMethod_ShouldReturn400()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/admin/api/try-request", new
        {
            url = "http://localhost:5000/api/test"
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("method is required");
    }

    [Fact]
    [DisplayName("無效的 method 應返回 400")]
    public async Task TryRequest_InvalidMethod_ShouldReturn400()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/admin/api/try-request", new
        {
            method = "INVALID",
            url = "http://localhost:5000/api/test"
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("method must be a valid HTTP method");
    }

    [Fact]
    [DisplayName("缺少 url 應返回 400")]
    public async Task TryRequest_MissingUrl_ShouldReturn400()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/admin/api/try-request", new
        {
            method = "GET"
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("url is required");
    }

    [Fact]
    [DisplayName("無效的 URL 格式應返回 400")]
    public async Task TryRequest_InvalidUrl_ShouldReturn400()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/admin/api/try-request", new
        {
            method = "GET",
            url = "not-a-url"
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("url must be an absolute HTTP(S) URL");
    }

    [Fact]
    [DisplayName("多個驗證錯誤應一次返回")]
    public async Task TryRequest_MultipleErrors_ShouldReturnAll()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/admin/api/try-request", new
        {
            method = "",
            url = ""
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("method is required");
        body.Should().Contain("url is required");
    }

    [Fact]
    [DisplayName("目標不可達應返回 502")]
    public async Task TryRequest_UnreachableTarget_ShouldReturn502()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/admin/api/try-request", new
        {
            method = "GET",
            url = "http://localhost:19999/not-reachable"
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadGateway);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("error");
        body.Should().Contain("elapsedMs");
    }

    [Fact]
    [DisplayName("支援所有合法 HTTP method")]
    public async Task TryRequest_AllValidMethods_ShouldAccept()
    {
        var client = _factory.CreateClient();
        var methods = new[] { "GET", "POST", "PUT", "PATCH", "DELETE", "HEAD", "OPTIONS" };

        foreach (var method in methods)
        {
            var response = await client.PostAsJsonAsync("/admin/api/try-request", new
            {
                method,
                url = "http://localhost:19999/test"
            });

            // Should not be 400 (validation passes, may get 502 because unreachable)
            response.StatusCode.Should().NotBe(HttpStatusCode.BadRequest,
                $"method '{method}' should be accepted as valid");
        }
    }

    private class TryRequestResult
    {
        public int StatusCode { get; set; }
        public Dictionary<string, string[]>? Headers { get; set; }
        public string? Body { get; set; }
        public long ElapsedMs { get; set; }
    }
}
