using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Mithya.Infrastructure.Data;

public class MithyaDbContextFactory : IDesignTimeDbContextFactory<MithyaDbContext>
{
    public MithyaDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<MithyaDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Database=mockserver;Username=mockserver;Password=mockserver123");

        return new MithyaDbContext(optionsBuilder.Options);
    }
}
