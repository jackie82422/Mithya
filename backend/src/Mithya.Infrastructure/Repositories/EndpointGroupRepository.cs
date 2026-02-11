using Microsoft.EntityFrameworkCore;
using Mithya.Core.Entities;
using Mithya.Core.Interfaces;
using Mithya.Infrastructure.Data;

namespace Mithya.Infrastructure.Repositories;

public class EndpointGroupRepository : IEndpointGroupRepository
{
    private readonly MithyaDbContext _context;

    public EndpointGroupRepository(MithyaDbContext context)
    {
        _context = context;
    }

    public async Task<EndpointGroup?> GetByIdAsync(Guid id)
    {
        return await _context.EndpointGroups.FindAsync(id);
    }

    public async Task<EndpointGroup?> GetByIdWithEndpointsAsync(Guid id)
    {
        return await _context.EndpointGroups
            .Include(g => g.Mappings)
                .ThenInclude(m => m.Endpoint)
            .FirstOrDefaultAsync(g => g.Id == id);
    }

    public async Task<IEnumerable<EndpointGroup>> GetAllAsync()
    {
        return await _context.EndpointGroups
            .Include(g => g.Mappings)
            .OrderByDescending(g => g.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<EndpointGroup>> GetGroupsByEndpointIdAsync(Guid endpointId)
    {
        return await _context.EndpointGroupMappings
            .Where(m => m.EndpointId == endpointId)
            .Include(m => m.Group)
            .Select(m => m.Group)
            .OrderBy(g => g.Name)
            .ToListAsync();
    }

    public async Task AddAsync(EndpointGroup group)
    {
        group.Id = Guid.NewGuid();
        group.CreatedAt = DateTime.UtcNow;
        group.UpdatedAt = DateTime.UtcNow;
        await _context.EndpointGroups.AddAsync(group);
    }

    public async Task UpdateAsync(EndpointGroup group)
    {
        group.UpdatedAt = DateTime.UtcNow;
        _context.EndpointGroups.Update(group);
        await Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid id)
    {
        var group = await _context.EndpointGroups.FindAsync(id);
        if (group != null)
            _context.EndpointGroups.Remove(group);
    }

    public async Task AddEndpointsAsync(Guid groupId, IEnumerable<Guid> endpointIds)
    {
        var existing = await _context.EndpointGroupMappings
            .Where(m => m.GroupId == groupId)
            .Select(m => m.EndpointId)
            .ToListAsync();

        var newMappings = endpointIds
            .Where(id => !existing.Contains(id))
            .Select(id => new EndpointGroupMapping
            {
                GroupId = groupId,
                EndpointId = id
            });

        await _context.EndpointGroupMappings.AddRangeAsync(newMappings);
    }

    public async Task RemoveEndpointAsync(Guid groupId, Guid endpointId)
    {
        var mapping = await _context.EndpointGroupMappings
            .FirstOrDefaultAsync(m => m.GroupId == groupId && m.EndpointId == endpointId);

        if (mapping != null)
            _context.EndpointGroupMappings.Remove(mapping);
    }

    public async Task<bool> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync() > 0;
    }
}
