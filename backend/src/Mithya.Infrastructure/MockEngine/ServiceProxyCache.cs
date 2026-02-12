using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Mithya.Core.Entities;
using Mithya.Core.Interfaces;

namespace Mithya.Infrastructure.MockEngine;

public interface IServiceProxyCache
{
    Task LoadAllAsync();
    Task ReloadAsync();
    ServiceProxy? GetActiveByServiceName(string serviceName);
    IReadOnlyList<ServiceProxy> GetAllActive();
}

public class ServiceProxyCache : IServiceProxyCache
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ConcurrentDictionary<string, ServiceProxy> _cache = new(StringComparer.OrdinalIgnoreCase);

    public ServiceProxyCache(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task LoadAllAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IServiceProxyRepository>();

        var proxies = await repo.GetAllAsync();

        _cache.Clear();

        foreach (var proxy in proxies)
        {
            if (!proxy.IsActive) continue;
            _cache[proxy.ServiceName] = proxy;
        }

        Console.WriteLine($"ServiceProxyCache loaded {_cache.Count} active service proxies");
    }

    public ServiceProxy? GetActiveByServiceName(string serviceName)
    {
        _cache.TryGetValue(serviceName, out var proxy);
        return proxy;
    }

    public IReadOnlyList<ServiceProxy> GetAllActive()
    {
        return _cache.Values.ToList().AsReadOnly();
    }

    public Task ReloadAsync()
    {
        return LoadAllAsync();
    }
}
