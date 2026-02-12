using Microsoft.EntityFrameworkCore;
using Mithya.Core.Entities;
using Mithya.Core.Interfaces;
using Mithya.Infrastructure.Data;

namespace Mithya.Infrastructure.Repositories;

public class ProxyConfigRepository : IProxyConfigRepository
{
    private readonly MithyaDbContext _context;

    public ProxyConfigRepository(MithyaDbContext context)
    {
        _context = context;
    }

    public async Task<ProxyConfig?> GetByIdAsync(Guid id)
    {
        return await _context.ProxyConfigs.FindAsync(id);
    }

    public async Task<IEnumerable<ProxyConfig>> GetAllAsync()
    {
        return await _context.ProxyConfigs.OrderByDescending(p => p.CreatedAt).ToListAsync();
    }

    public async Task<ProxyConfig?> GetActiveByEndpointIdAsync(Guid endpointId)
    {
        return await _context.ProxyConfigs
            .FirstOrDefaultAsync(p => p.EndpointId == endpointId && p.IsActive);
    }

    public async Task<ProxyConfig?> GetGlobalActiveAsync()
    {
        return await _context.ProxyConfigs
            .FirstOrDefaultAsync(p => p.EndpointId == null && p.IsActive);
    }

    public async Task AddAsync(ProxyConfig config)
    {
        config.Id = Guid.NewGuid();
        config.CreatedAt = DateTime.UtcNow;
        config.UpdatedAt = DateTime.UtcNow;
        await _context.ProxyConfigs.AddAsync(config);
    }

    public async Task UpdateAsync(ProxyConfig config)
    {
        config.UpdatedAt = DateTime.UtcNow;
        _context.ProxyConfigs.Update(config);
        await Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid id)
    {
        var config = await _context.ProxyConfigs.FindAsync(id);
        if (config != null)
            _context.ProxyConfigs.Remove(config);
    }

    public async Task<bool> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync() > 0;
    }
}
