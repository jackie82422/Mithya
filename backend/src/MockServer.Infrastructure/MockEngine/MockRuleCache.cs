using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using MockServer.Core.Entities;
using MockServer.Core.Interfaces;
using MockServer.Core.ValueObjects;

namespace MockServer.Infrastructure.MockEngine;

public class MockRuleCache : IMockRuleCache
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ConcurrentDictionary<Guid, CachedEndpoint> _cache = new();

    public MockRuleCache(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task LoadAllAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var endpointRepo = scope.ServiceProvider.GetRequiredService<IEndpointRepository>();
        var ruleRepo = scope.ServiceProvider.GetRequiredService<IRuleRepository>();

        var endpoints = await endpointRepo.GetAllActiveAsync();

        _cache.Clear();

        foreach (var endpoint in endpoints)
        {
            var rules = await ruleRepo.GetByEndpointIdAsync(endpoint.Id);
            var cached = ToCachedEndpoint(endpoint, rules);
            _cache[endpoint.Id] = cached;
        }

        Console.WriteLine($"MockRuleCache loaded {_cache.Count} active endpoints");
    }

    public IReadOnlyList<CachedEndpoint> GetAllEndpoints()
    {
        return _cache.Values.ToList().AsReadOnly();
    }

    public async Task ReloadEndpointAsync(Guid endpointId)
    {
        using var scope = _scopeFactory.CreateScope();
        var endpointRepo = scope.ServiceProvider.GetRequiredService<IEndpointRepository>();
        var ruleRepo = scope.ServiceProvider.GetRequiredService<IRuleRepository>();

        var endpoint = await endpointRepo.GetByIdAsync(endpointId);

        if (endpoint == null || !endpoint.IsActive)
        {
            _cache.TryRemove(endpointId, out _);
            return;
        }

        var rules = await ruleRepo.GetByEndpointIdAsync(endpointId);
        var cached = ToCachedEndpoint(endpoint, rules);
        _cache[endpointId] = cached;
    }

    public void RemoveEndpoint(Guid endpointId)
    {
        _cache.TryRemove(endpointId, out _);
    }

    public Task ReloadRulesForEndpointAsync(Guid endpointId)
    {
        return ReloadEndpointAsync(endpointId);
    }

    private static CachedEndpoint ToCachedEndpoint(MockEndpoint endpoint, IEnumerable<MockRule> rules)
    {
        return new CachedEndpoint
        {
            Id = endpoint.Id,
            Path = endpoint.Path,
            HttpMethod = endpoint.HttpMethod,
            Protocol = endpoint.Protocol,
            IsActive = endpoint.IsActive,
            DefaultResponse = endpoint.DefaultResponse,
            DefaultStatusCode = endpoint.DefaultStatusCode,
            Rules = rules
                .Where(r => r.IsActive)
                .OrderBy(r => r.Priority)
                .Select(r => new CachedRule
                {
                    Id = r.Id,
                    EndpointId = r.EndpointId,
                    RuleName = r.RuleName,
                    Priority = r.Priority,
                    Conditions = ParseConditions(r.MatchConditions),
                    ResponseStatusCode = r.ResponseStatusCode,
                    ResponseBody = r.ResponseBody,
                    ResponseHeaders = ParseHeaders(r.ResponseHeaders),
                    DelayMs = r.DelayMs,
                    IsTemplate = r.IsTemplate,
                    IsResponseHeadersTemplate = r.IsResponseHeadersTemplate
                })
                .ToList()
        };
    }

    private static List<MatchCondition> ParseConditions(string json)
    {
        try
        {
            return JsonConvert.DeserializeObject<List<MatchCondition>>(json) ?? new();
        }
        catch
        {
            return new();
        }
    }

    private static Dictionary<string, string>? ParseHeaders(string? json)
    {
        if (string.IsNullOrEmpty(json))
            return null;

        try
        {
            return JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
        }
        catch
        {
            return null;
        }
    }
}
