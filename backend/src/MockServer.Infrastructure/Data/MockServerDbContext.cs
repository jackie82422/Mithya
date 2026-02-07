using Microsoft.EntityFrameworkCore;
using MockServer.Core.Entities;

namespace MockServer.Infrastructure.Data;

public class MockServerDbContext : DbContext
{
    public MockServerDbContext(DbContextOptions<MockServerDbContext> options) : base(options)
    {
    }

    public DbSet<MockEndpoint> MockEndpoints => Set<MockEndpoint>();
    public DbSet<MockRule> MockRules => Set<MockRule>();
    public DbSet<MockRequestLog> MockRequestLogs => Set<MockRequestLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MockServerDbContext).Assembly);
    }
}
