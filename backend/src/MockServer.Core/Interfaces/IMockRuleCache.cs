using MockServer.Core.ValueObjects;

namespace MockServer.Core.Interfaces;

public interface IMockRuleCache
{
    Task LoadAllAsync();
    IReadOnlyList<CachedEndpoint> GetAllEndpoints();
    Task ReloadEndpointAsync(Guid endpointId);
    void RemoveEndpoint(Guid endpointId);
    Task ReloadRulesForEndpointAsync(Guid endpointId);
}
