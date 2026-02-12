using Mithya.Core.Entities;
using Mithya.Core.Interfaces;
using Mithya.Infrastructure.MockEngine;

namespace Mithya.Api.Endpoints;

public static class ScenarioApis
{
    public static void MapScenarioApis(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/admin/api/scenarios").WithTags("Scenarios");

        group.MapGet("/", async (IScenarioRepository repo) =>
        {
            var scenarios = await repo.GetAllAsync();
            return Results.Ok(scenarios);
        })
        .WithName("GetAllScenarios")
        .WithOpenApi();

        group.MapGet("/{id}", async (Guid id, IScenarioRepository repo) =>
        {
            var scenario = await repo.GetByIdWithStepsAsync(id);
            return scenario is not null
                ? Results.Ok(scenario)
                : Results.NotFound(new { error = "Scenario not found" });
        })
        .WithName("GetScenarioById")
        .WithOpenApi();

        group.MapPost("/", async (
            Scenario request,
            IScenarioRepository repo,
            IScenarioEngine engine) =>
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                return Results.BadRequest(new { error = "Name is required" });

            if (string.IsNullOrWhiteSpace(request.InitialState))
                return Results.BadRequest(new { error = "InitialState is required" });

            request.CurrentState = request.InitialState;
            request.Steps = new List<ScenarioStep>();

            await repo.AddAsync(request);
            await repo.SaveChangesAsync();
            await engine.ReloadAsync();

            return Results.Created($"/admin/api/scenarios/{request.Id}", request);
        })
        .WithName("CreateScenario")
        .WithOpenApi();

        group.MapPut("/{id}", async (
            Guid id,
            Scenario request,
            IScenarioRepository repo,
            IScenarioEngine engine) =>
        {
            var existing = await repo.GetByIdAsync(id);
            if (existing is null)
                return Results.NotFound(new { error = "Scenario not found" });

            existing.Name = request.Name;
            existing.Description = request.Description;
            existing.InitialState = request.InitialState;
            existing.IsActive = request.IsActive;

            await repo.UpdateAsync(existing);
            await repo.SaveChangesAsync();
            await engine.ReloadAsync();

            return Results.Ok(existing);
        })
        .WithName("UpdateScenario")
        .WithOpenApi();

        group.MapDelete("/{id}", async (
            Guid id,
            IScenarioRepository repo,
            IScenarioEngine engine) =>
        {
            var scenario = await repo.GetByIdAsync(id);
            if (scenario is null)
                return Results.NotFound(new { error = "Scenario not found" });

            await repo.DeleteAsync(id);
            await repo.SaveChangesAsync();
            await engine.ReloadAsync();

            return Results.NoContent();
        })
        .WithName("DeleteScenario")
        .WithOpenApi();

        group.MapMethods("/{id}/toggle", new[] { "PATCH" }, async (
            Guid id,
            IScenarioRepository repo,
            IScenarioEngine engine) =>
        {
            var scenario = await repo.GetByIdAsync(id);
            if (scenario is null)
                return Results.NotFound(new { error = "Scenario not found" });

            scenario.IsActive = !scenario.IsActive;
            await repo.UpdateAsync(scenario);
            await repo.SaveChangesAsync();
            await engine.ReloadAsync();

            return Results.Ok(scenario);
        })
        .WithName("ToggleScenario")
        .WithOpenApi();

        group.MapPost("/{id}/reset", async (
            Guid id,
            IScenarioEngine engine,
            IScenarioRepository repo) =>
        {
            var scenario = await repo.GetByIdAsync(id);
            if (scenario is null)
                return Results.NotFound(new { error = "Scenario not found" });

            await engine.ResetScenarioAsync(id);
            await engine.ReloadAsync();

            // Use in-memory state to avoid stale DbContext read
            // (ResetScenarioAsync writes via its own scope)
            scenario.CurrentState = scenario.InitialState;
            return Results.Ok(scenario);
        })
        .WithName("ResetScenario")
        .WithOpenApi();

        group.MapGet("/{id}/current-state", async (
            Guid id,
            IScenarioEngine engine,
            IScenarioRepository repo) =>
        {
            var scenario = await repo.GetByIdAsync(id);
            if (scenario is null)
                return Results.NotFound(new { error = "Scenario not found" });

            var state = engine.GetCurrentState(id) ?? scenario.CurrentState;
            return Results.Ok(new { scenarioId = id, currentState = state });
        })
        .WithName("GetScenarioCurrentState")
        .WithOpenApi();

        // Step management
        group.MapPost("/{id}/steps", async (
            Guid id,
            ScenarioStep step,
            IScenarioRepository repo,
            IScenarioStepRepository stepRepo,
            IScenarioEngine engine) =>
        {
            var scenario = await repo.GetByIdAsync(id);
            if (scenario is null)
                return Results.NotFound(new { error = "Scenario not found" });

            step.ScenarioId = id;
            await stepRepo.AddAsync(step);
            await stepRepo.SaveChangesAsync();
            await engine.ReloadAsync();

            return Results.Created($"/admin/api/scenarios/{id}/steps/{step.Id}", step);
        })
        .WithName("AddScenarioStep")
        .WithOpenApi();

        group.MapPut("/{id}/steps/{stepId}", async (
            Guid id,
            Guid stepId,
            ScenarioStep request,
            IScenarioStepRepository stepRepo,
            IScenarioEngine engine) =>
        {
            var step = await stepRepo.GetByIdAsync(stepId);
            if (step is null || step.ScenarioId != id)
                return Results.NotFound(new { error = "Scenario step not found" });

            step.StateName = request.StateName;
            step.EndpointId = request.EndpointId;
            step.MatchConditions = request.MatchConditions;
            step.ResponseStatusCode = request.ResponseStatusCode;
            step.ResponseBody = request.ResponseBody;
            step.ResponseHeaders = request.ResponseHeaders;
            step.IsTemplate = request.IsTemplate;
            step.DelayMs = request.DelayMs;
            step.NextState = request.NextState;
            step.Priority = request.Priority;

            await stepRepo.UpdateAsync(step);
            await stepRepo.SaveChangesAsync();
            await engine.ReloadAsync();

            return Results.Ok(step);
        })
        .WithName("UpdateScenarioStep")
        .WithOpenApi();

        group.MapDelete("/{id}/steps/{stepId}", async (
            Guid id,
            Guid stepId,
            IScenarioStepRepository stepRepo,
            IScenarioEngine engine) =>
        {
            var step = await stepRepo.GetByIdAsync(stepId);
            if (step is null || step.ScenarioId != id)
                return Results.NotFound(new { error = "Scenario step not found" });

            await stepRepo.DeleteAsync(stepId);
            await stepRepo.SaveChangesAsync();
            await engine.ReloadAsync();

            return Results.NoContent();
        })
        .WithName("DeleteScenarioStep")
        .WithOpenApi();
    }
}
