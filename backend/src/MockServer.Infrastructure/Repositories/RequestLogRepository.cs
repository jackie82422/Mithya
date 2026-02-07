using Microsoft.EntityFrameworkCore;
using MockServer.Core.Entities;
using MockServer.Core.Interfaces;
using MockServer.Infrastructure.Data;

namespace MockServer.Infrastructure.Repositories;

public class RequestLogRepository : IRequestLogRepository
{
    private readonly MockServerDbContext _context;

    public RequestLogRepository(MockServerDbContext context)
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

    public async Task<bool> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync() > 0;
    }
}
