using Microsoft.EntityFrameworkCore;
using MockServer.Core.Entities;
using MockServer.Core.Interfaces;
using MockServer.Infrastructure.Data;

namespace MockServer.Infrastructure.Repositories;

public class ServiceProxyRepository : IServiceProxyRepository
{
    private readonly MockServerDbContext _context;

    public ServiceProxyRepository(MockServerDbContext context)
    {
        _context = context;
    }

    public async Task<ServiceProxy?> GetByIdAsync(Guid id)
    {
        return await _context.ServiceProxies.FindAsync(id);
    }

    public async Task<ServiceProxy?> GetByServiceNameAsync(string serviceName)
    {
        return await _context.ServiceProxies
            .FirstOrDefaultAsync(p => p.ServiceName == serviceName);
    }

    public async Task<IEnumerable<ServiceProxy>> GetAllAsync()
    {
        return await _context.ServiceProxies.OrderByDescending(p => p.CreatedAt).ToListAsync();
    }

    public async Task AddAsync(ServiceProxy proxy)
    {
        proxy.Id = Guid.NewGuid();
        proxy.CreatedAt = DateTime.UtcNow;
        proxy.UpdatedAt = DateTime.UtcNow;
        await _context.ServiceProxies.AddAsync(proxy);
    }

    public async Task UpdateAsync(ServiceProxy proxy)
    {
        proxy.UpdatedAt = DateTime.UtcNow;
        _context.ServiceProxies.Update(proxy);
        await Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid id)
    {
        var proxy = await _context.ServiceProxies.FindAsync(id);
        if (proxy != null)
            _context.ServiceProxies.Remove(proxy);
    }

    public async Task<bool> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync() > 0;
    }
}
