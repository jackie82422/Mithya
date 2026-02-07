using MockServer.Core.Entities;

namespace MockServer.Core.Interfaces;

public interface IRuleRepository
{
    Task<MockRule?> GetByIdAsync(Guid id);
    Task<IEnumerable<MockRule>> GetByEndpointIdAsync(Guid endpointId);
    Task AddAsync(MockRule rule);
    Task UpdateAsync(MockRule rule);
    Task DeleteAsync(Guid id);
    Task<bool> SaveChangesAsync();
}
