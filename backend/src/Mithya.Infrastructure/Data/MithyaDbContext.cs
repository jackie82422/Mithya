using Microsoft.EntityFrameworkCore;
using Mithya.Core.Entities;

namespace Mithya.Infrastructure.Data;

public class MithyaDbContext : DbContext
{
    public MithyaDbContext(DbContextOptions<MithyaDbContext> options) : base(options)
    {
    }

    public DbSet<MockEndpoint> MockEndpoints => Set<MockEndpoint>();
    public DbSet<MockRule> MockRules => Set<MockRule>();
    public DbSet<MockRequestLog> MockRequestLogs => Set<MockRequestLog>();
    public DbSet<ProxyConfig> ProxyConfigs => Set<ProxyConfig>();
    public DbSet<ServiceProxy> ServiceProxies => Set<ServiceProxy>();
    public DbSet<Scenario> Scenarios => Set<Scenario>();
    public DbSet<ScenarioStep> ScenarioSteps => Set<ScenarioStep>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MithyaDbContext).Assembly);
    }
}
