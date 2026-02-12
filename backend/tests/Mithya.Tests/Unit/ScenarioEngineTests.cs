using System.ComponentModel;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mithya.Core.Entities;
using Mithya.Core.Interfaces;
using Mithya.Infrastructure.Data;
using Mithya.Infrastructure.MockEngine;
using Mithya.Infrastructure.Repositories;
using Xunit;

namespace Mithya.Tests.Unit;

public class ScenarioEngineTests
{
    private (IServiceScopeFactory factory, ServiceProvider sp) BuildServices(string dbName)
    {
        var services = new ServiceCollection();
        services.AddDbContext<MithyaDbContext>(options =>
            options.UseInMemoryDatabase(dbName));
        services.AddScoped<IScenarioRepository, ScenarioRepository>();
        services.AddScoped<IScenarioStepRepository, ScenarioStepRepository>();

        var sp = services.BuildServiceProvider();

        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MithyaDbContext>();
        db.Database.EnsureCreated();

        return (sp.GetRequiredService<IServiceScopeFactory>(), sp);
    }

    [Fact]
    [DisplayName("TryMatchAsync 應匹配當前狀態的步驟")]
    public async Task TryMatch_ShouldMatchCurrentStateStep()
    {
        // Arrange
        var (factory, sp) = BuildServices("ScenarioTest_Match");
        var endpointId = Guid.NewGuid();
        var scenarioId = Guid.NewGuid();

        using (var scope = sp.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MithyaDbContext>();
            var scenario = new Scenario
            {
                Id = scenarioId,
                Name = "Test Scenario",
                InitialState = "start",
                CurrentState = "start",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            db.Scenarios.Add(scenario);

            db.ScenarioSteps.Add(new ScenarioStep
            {
                Id = Guid.NewGuid(),
                ScenarioId = scenarioId,
                StateName = "start",
                EndpointId = endpointId,
                ResponseStatusCode = 200,
                ResponseBody = "{\"state\": \"start\"}",
                NextState = "step2",
                Priority = 1
            });
            await db.SaveChangesAsync();
        }

        var engine = new ScenarioEngine(factory);
        await engine.LoadAllAsync();

        var context = new MockRequestContext
        {
            Method = "GET",
            Path = "/api/test",
            Headers = new Dictionary<string, string>(),
            QueryParams = new Dictionary<string, string>()
        };

        // Act
        var result = await engine.TryMatchAsync(context, endpointId);

        // Assert
        result.Should().NotBeNull();
        result!.Step.ResponseBody.Should().Contain("start");
        engine.GetCurrentState(scenarioId).Should().Be("step2");
    }

    [Fact]
    [DisplayName("TryMatchAsync 不應匹配其他狀態的步驟")]
    public async Task TryMatch_ShouldNotMatchDifferentState()
    {
        // Arrange
        var (factory, sp) = BuildServices("ScenarioTest_NoMatch");
        var endpointId = Guid.NewGuid();
        var scenarioId = Guid.NewGuid();

        using (var scope = sp.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MithyaDbContext>();
            var scenario = new Scenario
            {
                Id = scenarioId,
                Name = "Test Scenario",
                InitialState = "start",
                CurrentState = "start",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            db.Scenarios.Add(scenario);

            db.ScenarioSteps.Add(new ScenarioStep
            {
                Id = Guid.NewGuid(),
                ScenarioId = scenarioId,
                StateName = "other_state",
                EndpointId = endpointId,
                ResponseStatusCode = 200,
                ResponseBody = "{}",
                Priority = 1
            });
            await db.SaveChangesAsync();
        }

        var engine = new ScenarioEngine(factory);
        await engine.LoadAllAsync();

        var context = new MockRequestContext
        {
            Method = "GET",
            Path = "/api/test",
            Headers = new Dictionary<string, string>(),
            QueryParams = new Dictionary<string, string>()
        };

        // Act
        var result = await engine.TryMatchAsync(context, endpointId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    [DisplayName("ResetScenarioAsync 應重置為初始狀態")]
    public async Task ResetScenario_ShouldResetToInitialState()
    {
        // Arrange
        var (factory, sp) = BuildServices("ScenarioTest_Reset");
        var endpointId = Guid.NewGuid();
        var scenarioId = Guid.NewGuid();

        using (var scope = sp.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MithyaDbContext>();
            var scenario = new Scenario
            {
                Id = scenarioId,
                Name = "Test Scenario",
                InitialState = "start",
                CurrentState = "start",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            db.Scenarios.Add(scenario);

            db.ScenarioSteps.Add(new ScenarioStep
            {
                Id = Guid.NewGuid(),
                ScenarioId = scenarioId,
                StateName = "start",
                EndpointId = endpointId,
                ResponseStatusCode = 200,
                ResponseBody = "{}",
                NextState = "step2",
                Priority = 1
            });
            await db.SaveChangesAsync();
        }

        var engine = new ScenarioEngine(factory);
        await engine.LoadAllAsync();

        var context = new MockRequestContext
        {
            Method = "GET",
            Path = "/api/test",
            Headers = new Dictionary<string, string>(),
            QueryParams = new Dictionary<string, string>()
        };

        // Trigger state transition
        await engine.TryMatchAsync(context, endpointId);
        engine.GetCurrentState(scenarioId).Should().Be("step2");

        // Act
        await engine.ResetScenarioAsync(scenarioId);

        // Assert
        engine.GetCurrentState(scenarioId).Should().Be("start");
    }

    [Fact]
    [DisplayName("非啟用的情境不應被載入")]
    public async Task LoadAll_ShouldSkipInactiveScenarios()
    {
        // Arrange
        var (factory, sp) = BuildServices("ScenarioTest_Inactive");
        var endpointId = Guid.NewGuid();

        using (var scope = sp.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MithyaDbContext>();
            db.Scenarios.Add(new Scenario
            {
                Id = Guid.NewGuid(),
                Name = "Inactive Scenario",
                InitialState = "start",
                CurrentState = "start",
                IsActive = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Steps = new List<ScenarioStep>
                {
                    new ScenarioStep
                    {
                        Id = Guid.NewGuid(),
                        StateName = "start",
                        EndpointId = endpointId,
                        ResponseStatusCode = 200,
                        ResponseBody = "{}",
                        Priority = 1
                    }
                }
            });
            await db.SaveChangesAsync();
        }

        var engine = new ScenarioEngine(factory);
        await engine.LoadAllAsync();

        var context = new MockRequestContext
        {
            Method = "GET",
            Path = "/api/test",
            Headers = new Dictionary<string, string>(),
            QueryParams = new Dictionary<string, string>()
        };

        // Act
        var result = await engine.TryMatchAsync(context, endpointId);

        // Assert
        result.Should().BeNull();
    }
}
