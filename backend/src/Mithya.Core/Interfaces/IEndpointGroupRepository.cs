using Mithya.Core.Entities;

namespace Mithya.Core.Interfaces;

public interface IEndpointGroupRepository
{
    Task<EndpointGroup?> GetByIdAsync(Guid id);
    Task<EndpointGroup?> GetByIdWithEndpointsAsync(Guid id);
    Task<IEnumerable<EndpointGroup>> GetAllAsync();
    Task<IEnumerable<EndpointGroup>> GetGroupsByEndpointIdAsync(Guid endpointId);
    Task AddAsync(EndpointGroup group);
    Task UpdateAsync(EndpointGroup group);
    Task DeleteAsync(Guid id);
    Task AddEndpointsAsync(Guid groupId, IEnumerable<Guid> endpointIds);
    Task RemoveEndpointAsync(Guid groupId, Guid endpointId);
    Task<bool> SaveChangesAsync();
}
