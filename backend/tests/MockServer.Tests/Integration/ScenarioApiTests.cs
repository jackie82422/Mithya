using System.ComponentModel;
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MockServer.Core.Entities;
using MockServer.Infrastructure.Data;
using Xunit;

namespace MockServer.Tests.Integration;

public class ScenarioApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ScenarioApiTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<MockServerDbContext>));
                if (descriptor != null)
                    services.Remove(descriptor);

                services.AddDbContext<MockServerDbContext>(options =>
                    options.UseInMemoryDatabase("TestDb_Scenario"));

                var sp = services.BuildServiceProvider();
                using var scope = sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<MockServerDbContext>();
                db.Database.EnsureCreated();
            });
        });
    }

    [Fact]
    [DisplayName("建立 Scenario 應返回 201")]
    public async Task CreateScenario_ShouldReturn201()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/admin/api/scenarios", new
        {
            name = "Login Flow",
            description = "User login scenario",
            initialState = "logged_out",
            isActive = true
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var scenario = await response.Content.ReadFromJsonAsync<Scenario>();
        scenario.Should().NotBeNull();
        scenario!.Name.Should().Be("Login Flow");
        scenario.CurrentState.Should().Be("logged_out");
    }

    [Fact]
    [DisplayName("建立 Scenario 缺少 Name 應返回 400")]
    public async Task CreateScenario_MissingName_ShouldReturn400()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/admin/api/scenarios", new
        {
            name = "",
            initialState = "start"
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    [DisplayName("取得所有 Scenario 應返回列表")]
    public async Task GetAllScenarios_ShouldReturnList()
    {
        // Arrange
        var client = _factory.CreateClient();

        await client.PostAsJsonAsync("/admin/api/scenarios", new
        {
            name = "List Test Scenario",
            initialState = "start",
            isActive = true
        });

        // Act
        var response = await client.GetAsync("/admin/api/scenarios");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var scenarios = await response.Content.ReadFromJsonAsync<List<Scenario>>();
        scenarios.Should().NotBeNull();
        scenarios!.Should().Contain(s => s.Name == "List Test Scenario");
    }

    [Fact]
    [DisplayName("Toggle Scenario 應切換 IsActive")]
    public async Task ToggleScenario_ShouldToggleIsActive()
    {
        // Arrange
        var client = _factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync("/admin/api/scenarios", new
        {
            name = "Toggle Test",
            initialState = "start",
            isActive = true
        });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<Scenario>();

        // Act
        var patchRequest = new HttpRequestMessage(HttpMethod.Patch,
            $"/admin/api/scenarios/{created!.Id}/toggle");
        var response = await client.SendAsync(patchRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var toggled = await response.Content.ReadFromJsonAsync<Scenario>();
        toggled!.IsActive.Should().BeFalse();
    }

    [Fact]
    [DisplayName("Reset Scenario 應重置為初始狀態")]
    public async Task ResetScenario_ShouldResetState()
    {
        // Arrange
        var client = _factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync("/admin/api/scenarios", new
        {
            name = "Reset Test",
            initialState = "start",
            isActive = true
        });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<Scenario>();

        // Act
        var response = await client.PostAsync($"/admin/api/scenarios/{created!.Id}/reset", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var reset = await response.Content.ReadFromJsonAsync<Scenario>();
        reset!.CurrentState.Should().Be("start");
    }

    [Fact]
    [DisplayName("新增 Step 到 Scenario 應返回 201")]
    public async Task AddStep_ShouldReturn201()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Create endpoint first
        var endpointResponse = await client.PostAsJsonAsync("/admin/api/endpoints", new
        {
            name = "Step Endpoint",
            serviceName = "Test",
            protocol = 1,
            path = "/api/scenario-step-test",
            httpMethod = "POST"
        });
        endpointResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var endpoint = await endpointResponse.Content.ReadFromJsonAsync<MockEndpoint>();

        // Create scenario
        var scenarioResponse = await client.PostAsJsonAsync("/admin/api/scenarios", new
        {
            name = "Step Test",
            initialState = "start",
            isActive = true
        });
        scenarioResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var scenario = await scenarioResponse.Content.ReadFromJsonAsync<Scenario>();

        // Act
        var response = await client.PostAsJsonAsync($"/admin/api/scenarios/{scenario!.Id}/steps", new
        {
            stateName = "start",
            endpointId = endpoint!.Id,
            responseStatusCode = 200,
            responseBody = "{\"message\": \"hello\"}",
            nextState = "step2",
            priority = 1
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var step = await response.Content.ReadFromJsonAsync<ScenarioStep>();
        step.Should().NotBeNull();
        step!.StateName.Should().Be("start");
        step.NextState.Should().Be("step2");
    }

    [Fact]
    [DisplayName("刪除 Scenario 應返回 204")]
    public async Task DeleteScenario_ShouldReturn204()
    {
        // Arrange
        var client = _factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync("/admin/api/scenarios", new
        {
            name = "Delete Test",
            initialState = "start",
            isActive = true
        });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<Scenario>();

        // Act
        var response = await client.DeleteAsync($"/admin/api/scenarios/{created!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await client.GetAsync($"/admin/api/scenarios/{created.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    [DisplayName("取得 Scenario 當前狀態")]
    public async Task GetCurrentState_ShouldReturnState()
    {
        // Arrange
        var client = _factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync("/admin/api/scenarios", new
        {
            name = "State Test",
            initialState = "initial",
            isActive = true
        });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<Scenario>();

        // Act
        var response = await client.GetAsync($"/admin/api/scenarios/{created!.Id}/current-state");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("initial");
    }
}
