using MockServer.Core.Entities;

namespace MockServer.Core.Interfaces;

public interface IRequestLogRepository
{
    Task<IEnumerable<MockRequestLog>> GetLogsAsync(int limit = 100);
    Task<IEnumerable<MockRequestLog>> GetLogsByEndpointAsync(Guid endpointId, int limit = 100);
    Task AddAsync(MockRequestLog log);
    Task DeleteAllAsync();
    Task<bool> SaveChangesAsync();
}
