using MockServer.Core.Entities;

namespace MockServer.Core.Interfaces;

public interface IServiceProxyRepository
{
    Task<ServiceProxy?> GetByIdAsync(Guid id);
    Task<ServiceProxy?> GetByServiceNameAsync(string serviceName);
    Task<IEnumerable<ServiceProxy>> GetAllAsync();
    Task AddAsync(ServiceProxy proxy);
    Task UpdateAsync(ServiceProxy proxy);
    Task DeleteAsync(Guid id);
    Task<bool> SaveChangesAsync();
}
