using Microsoft.EntityFrameworkCore;
using Mithya.Core.Entities;
using Mithya.Core.Interfaces;
using Mithya.Infrastructure.Data;

namespace Mithya.Infrastructure.Repositories;

public class RequestLogRepository : IRequestLogRepository
{
    private readonly MithyaDbContext _context;

    public RequestLogRepository(MithyaDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<MockRequestLog>> GetLogsAsync(int limit = 100)
    {
        return await _context.MockRequestLogs
            .OrderByDescending(l => l.Timestamp)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<IEnumerable<MockRequestLog>> GetLogsByEndpointAsync(Guid endpointId, int limit = 100)
    {
        return await _context.MockRequestLogs
            .Where(l => l.EndpointId == endpointId)
            .OrderByDescending(l => l.Timestamp)
            .Take(limit)
            .ToListAsync();
    }

    public async Task AddAsync(MockRequestLog log)
    {
        log.Id = Guid.NewGuid();
        log.Timestamp = DateTime.UtcNow;
        await _context.MockRequestLogs.AddAsync(log);
    }

    public async Task DeleteAllAsync()
    {
        _context.MockRequestLogs.RemoveRange(_context.MockRequestLogs);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync() > 0;
    }
}
