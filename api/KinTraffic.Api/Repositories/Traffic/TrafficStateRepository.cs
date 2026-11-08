using KinTraffic.Api.Models;
using KinTraffic.Api.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KinTraffic.Api.Repositories.Traffic;

public class TrafficStateRepository(KinTrafficDbContext dbContext)
{
    public async Task<IReadOnlyList<TrafficState>> GetTrafficStatesAsync(CancellationToken cancellationToken = default) =>
        await dbContext.TrafficStates
            .AsNoTracking()
            .OrderByDescending(state => state.RecordedAt)
            .ToListAsync(cancellationToken);
}