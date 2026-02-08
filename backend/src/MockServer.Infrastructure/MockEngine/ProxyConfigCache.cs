using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using MockServer.Core.Entities;
using MockServer.Core.Interfaces;

namespace MockServer.Infrastructure.MockEngine;

public interface IProxyConfigCache
{
    Task LoadAllAsync();
    ProxyConfig? GetActiveForEndpoint(Guid endpointId);
    ProxyConfig? GetGlobalActive();
    Task ReloadAsync();
}

public class ProxyConfigCache : IProxyConfigCache
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ConcurrentDictionary<Guid, ProxyConfig> _endpointConfigs = new();
    private ProxyConfig? _globalConfig;

    public ProxyConfigCache(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task LoadAllAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IProxyConfigRepository>();

        var configs = await repo.GetAllAsync();

        _endpointConfigs.Clear();
        _globalConfig = null;

        foreach (var config in configs)
        {
            if (!config.IsActive) continue;

            if (config.EndpointId.HasValue)
                _endpointConfigs[config.EndpointId.Value] = config;
            else
                _globalConfig ??= config;
        }

        Console.WriteLine($"ProxyConfigCache loaded {_endpointConfigs.Count} endpoint configs, global: {(_globalConfig != null ? "yes" : "no")}");
    }

    public ProxyConfig? GetActiveForEndpoint(Guid endpointId)
    {
        _endpointConfigs.TryGetValue(endpointId, out var config);
        return config;
    }

    public ProxyConfig? GetGlobalActive()
    {
        return _globalConfig;
    }

    public Task ReloadAsync()
    {
        return LoadAllAsync();
    }
}
