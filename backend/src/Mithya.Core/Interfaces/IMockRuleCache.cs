using Mithya.Core.ValueObjects;

namespace Mithya.Core.Interfaces;

public interface IMockRuleCache
{
    Task LoadAllAsync();
    IReadOnlyList<CachedEndpoint> GetAllEndpoints();
    Task ReloadEndpointAsync(Guid endpointId);
    void RemoveEndpoint(Guid endpointId);
    Task ReloadRulesForEndpointAsync(Guid endpointId);
}
