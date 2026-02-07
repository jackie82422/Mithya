using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MockServer.Infrastructure.Data;

public class MockServerDbContextFactory : IDesignTimeDbContextFactory<MockServerDbContext>
{
    public MockServerDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<MockServerDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Database=mockserver;Username=mockserver;Password=mockserver123");

        return new MockServerDbContext(optionsBuilder.Options);
    }
}
