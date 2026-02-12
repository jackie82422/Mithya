using Microsoft.EntityFrameworkCore;
using Mithya.Core.Entities;
using Mithya.Core.Interfaces;
using Mithya.Infrastructure.Data;

namespace Mithya.Infrastructure.Repositories;

public class RuleRepository : IRuleRepository
{
    private readonly MithyaDbContext _context;

    public RuleRepository(MithyaDbContext context)
    {
        _context = context;
    }

    public async Task<MockRule?> GetByIdAsync(Guid id)
    {
        return await _context.MockRules
            .Include(r => r.Endpoint)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<IEnumerable<MockRule>> GetByEndpointIdAsync(Guid endpointId)
    {
        return await _context.MockRules
            .Where(r => r.EndpointId == endpointId)
            .OrderBy(r => r.Priority)
            .ToListAsync();
    }

    public async Task AddAsync(MockRule rule)
    {
        rule.Id = Guid.NewGuid();
        rule.CreatedAt = DateTime.UtcNow;
        rule.UpdatedAt = DateTime.UtcNow;
        await _context.MockRules.AddAsync(rule);
    }

    public Task UpdateAsync(MockRule rule)
    {
        rule.UpdatedAt = DateTime.UtcNow;
        _context.MockRules.Update(rule);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid id)
    {
        var rule = await _context.MockRules.FindAsync(id);
        if (rule != null)
        {
            _context.MockRules.Remove(rule);
        }
    }

    public async Task<bool> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync() > 0;
    }
}
