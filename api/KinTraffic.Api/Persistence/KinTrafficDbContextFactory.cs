using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace KinTraffic.Api.Persistence;

public sealed class KinTrafficDbContextFactory : IDesignTimeDbContextFactory<KinTrafficDbContext>
{
    public KinTrafficDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__KinTraffic");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Set ConnectionStrings__KinTraffic before running EF Core migration commands.");
        }

        var options = new DbContextOptionsBuilder<KinTrafficDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new KinTrafficDbContext(options);
    }
}