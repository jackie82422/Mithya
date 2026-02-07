using Microsoft.EntityFrameworkCore;
using MockServer.Core.Entities;
using MockServer.Core.Interfaces;
using MockServer.Infrastructure.Data;

namespace MockServer.Infrastructure.Repositories;

public class EndpointRepository : IEndpointRepository
{
    private readonly MockServerDbContext _context;

    public EndpointRepository(MockServerDbContext context)
    {
        _context = context;
    }

    public async Task<MockEndpoint?> GetByIdAsync(Guid id)
    {
        return await _context.MockEndpoints
            .Include(e => e.Rules)
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<IEnumerable<MockEndpoint>> GetAllAsync()
    {
        return await _context.MockEndpoints
            .Include(e => e.Rules)
            .OrderBy(e => e.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<MockEndpoint>> GetAllActiveAsync()
    {
        return await _context.MockEndpoints
            .Include(e => e.Rules)
            .Where(e => e.IsActive)
            .OrderBy(e => e.CreatedAt)
            .ToListAsync();
    }

    public async Task AddAsync(MockEndpoint endpoint)
    {
        endpoint.Id = Guid.NewGuid();
        endpoint.CreatedAt = DateTime.UtcNow;
        endpoint.UpdatedAt = DateTime.UtcNow;
        await _context.MockEndpoints.AddAsync(endpoint);
    }

    public Task UpdateAsync(MockEndpoint endpoint)
    {
        endpoint.UpdatedAt = DateTime.UtcNow;
        _context.MockEndpoints.Update(endpoint);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid id)
    {
        var endpoint = await _context.MockEndpoints.FindAsync(id);
        if (endpoint != null)
        {
            _context.MockEndpoints.Remove(endpoint);
        }
    }

    public async Task<bool> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync() > 0;
    }
}
