using Mithya.Core.Entities;

namespace Mithya.Core.Interfaces;

public interface IEndpointRepository
{
    Task<MockEndpoint?> GetByIdAsync(Guid id);
    Task<IEnumerable<MockEndpoint>> GetAllAsync();
    Task<IEnumerable<MockEndpoint>> GetAllActiveAsync();
    Task AddAsync(MockEndpoint endpoint);
    Task UpdateAsync(MockEndpoint endpoint);
    Task DeleteAsync(Guid id);
    Task<bool> SaveChangesAsync();
}
