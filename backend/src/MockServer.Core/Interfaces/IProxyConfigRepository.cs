using MockServer.Core.Entities;

namespace MockServer.Core.Interfaces;

public interface IProxyConfigRepository
{
    Task<ProxyConfig?> GetByIdAsync(Guid id);
    Task<IEnumerable<ProxyConfig>> GetAllAsync();
    Task<ProxyConfig?> GetActiveByEndpointIdAsync(Guid endpointId);
    Task<ProxyConfig?> GetGlobalActiveAsync();
    Task AddAsync(ProxyConfig config);
    Task UpdateAsync(ProxyConfig config);
    Task DeleteAsync(Guid id);
    Task<bool> SaveChangesAsync();
}
