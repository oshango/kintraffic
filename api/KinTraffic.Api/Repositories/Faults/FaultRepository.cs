using KinTraffic.Api.Models;
using KinTraffic.Api.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KinTraffic.Api.Repositories.Faults;

public class FaultRepository(KinTrafficDbContext dbContext)
{
    public async Task<IReadOnlyList<Fault>> GetFaultsAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Faults
            .AsNoTracking()
            .Include(fault => fault.Device)
            .OrderByDescending(fault => fault.CreatedAt)
            .ToListAsync(cancellationToken);
}
