using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using MockServer.Core.Entities;
using MockServer.Core.Interfaces;
using Newtonsoft.Json;

namespace MockServer.Infrastructure.MockEngine;

public class ScenarioMatchResult
{
    public Scenario Scenario { get; set; } = null!;
    public ScenarioStep Step { get; set; } = null!;
}

public interface IScenarioEngine
{
    Task LoadAllAsync();
    Task<ScenarioMatchResult?> TryMatchAsync(MockRequestContext context, Guid endpointId);
    Task ResetScenarioAsync(Guid scenarioId);
    string? GetCurrentState(Guid scenarioId);
    Task ReloadAsync();
}

public class ScenarioEngine : IScenarioEngine
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ConcurrentDictionary<Guid, string> _scenarioStates = new();
    private List<Scenario> _activeScenarios = new();

    public ScenarioEngine(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task LoadAllAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IScenarioRepository>();

        var scenarios = (await repo.GetActiveWithStepsAsync()).ToList();
        _activeScenarios = scenarios;
        _scenarioStates.Clear();

        foreach (var scenario in scenarios)
        {
            _scenarioStates[scenario.Id] = scenario.CurrentState;
        }

        Console.WriteLine($"ScenarioEngine loaded {scenarios.Count} active scenarios");
    }

    public Task<ScenarioMatchResult?> TryMatchAsync(MockRequestContext context, Guid endpointId)
    {
        foreach (var scenario in _activeScenarios)
        {
            if (!_scenarioStates.TryGetValue(scenario.Id, out var currentState))
                continue;

            var matchingSteps = scenario.Steps
                .Where(s => s.StateName == currentState && s.EndpointId == endpointId)
                .OrderBy(s => s.Priority)
                .ToList();

            foreach (var step in matchingSteps)
            {
                if (EvaluateStepConditions(step, context))
                {
                    // Transition state
                    if (!string.IsNullOrEmpty(step.NextState))
                    {
                        _scenarioStates[scenario.Id] = step.NextState;
                        _ = PersistStateAsync(scenario.Id, step.NextState);
                    }

                    return Task.FromResult<ScenarioMatchResult?>(new ScenarioMatchResult
                    {
                        Scenario = scenario,
                        Step = step
                    });
                }
            }
        }

        return Task.FromResult<ScenarioMatchResult?>(null);
    }

    public async Task ResetScenarioAsync(Guid scenarioId)
    {
        using var scope = _scopeFactory.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IScenarioRepository>();

        var scenario = await repo.GetByIdAsync(scenarioId);
        if (scenario == null) return;

        _scenarioStates[scenarioId] = scenario.InitialState;
        scenario.CurrentState = scenario.InitialState;
        await repo.UpdateAsync(scenario);
        await repo.SaveChangesAsync();
    }

    public string? GetCurrentState(Guid scenarioId)
    {
        _scenarioStates.TryGetValue(scenarioId, out var state);
        return state;
    }

    public Task ReloadAsync()
    {
        return LoadAllAsync();
    }

    private bool EvaluateStepConditions(ScenarioStep step, MockRequestContext context)
    {
        if (string.IsNullOrEmpty(step.MatchConditions) || step.MatchConditions == "[]")
            return true;

        try
        {
            var conditions = JsonConvert.DeserializeObject<List<MatchConditionData>>(step.MatchConditions);
            if (conditions == null || conditions.Count == 0)
                return true;

            return conditions.All(c => EvaluateCondition(c, context));
        }
        catch
        {
            return true;
        }
    }

    private static bool EvaluateCondition(MatchConditionData condition, MockRequestContext context)
    {
        var actual = ExtractFieldValue(condition.FieldSource, condition.FieldName, context);

        return condition.Operator switch
        {
            "Equals" or "equals" => string.Equals(actual, condition.ExpectedValue, StringComparison.OrdinalIgnoreCase),
            "Contains" or "contains" => actual?.Contains(condition.ExpectedValue ?? "", StringComparison.OrdinalIgnoreCase) ?? false,
            "Exists" or "exists" => actual != null,
            _ => true
        };
    }

    private static string? ExtractFieldValue(string fieldSource, string fieldName, MockRequestContext context)
    {
        return fieldSource.ToLower() switch
        {
            "header" => context.Headers.TryGetValue(fieldName, out var h) ? h : null,
            "query" or "queryparam" => context.QueryParams.TryGetValue(fieldName, out var q) ? q : null,
            "body" => context.Body,
            "path" => context.Path,
            _ => null
        };
    }

    private async Task PersistStateAsync(Guid scenarioId, string newState)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IScenarioRepository>();

            var scenario = await repo.GetByIdAsync(scenarioId);
            if (scenario != null)
            {
                scenario.CurrentState = newState;
                await repo.UpdateAsync(scenario);
                await repo.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error persisting scenario state: {ex.Message}");
        }
    }

    private class MatchConditionData
    {
        public string FieldSource { get; set; } = string.Empty;
        public string FieldName { get; set; } = string.Empty;
        public string Operator { get; set; } = "Equals";
        public string? ExpectedValue { get; set; }
    }
}
