using KinTraffic.Api.Models;
using KinTraffic.Api.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KinTraffic.Api.Repositories.Traffic;

public class SqlTrafficRepository(KinTrafficDbContext dbContext) : ITrafficRepository
{
    public async Task<IReadOnlyList<TrafficSignal>> GetSignalsAsync() =>
        await dbContext.TrafficSignals
            .AsNoTracking()
            .OrderBy(signal => signal.Name)
            .ToListAsync();

    public Task<TrafficSignal?> GetSignalByIdAsync(string id) =>
        dbContext.TrafficSignals.AsNoTracking().FirstOrDefaultAsync(signal => signal.Id == id);
}