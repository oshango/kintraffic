using KinTraffic.Api.Models;
using KinTraffic.Api.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KinTraffic.Api.Repositories.Assets;

public class AssetRepository(KinTrafficDbContext dbContext)
{
    public async Task<IReadOnlyList<Device>> GetAssetsAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Devices
            .AsNoTracking()
            .OrderBy(device => device.Name)
            .ToListAsync(cancellationToken);
}
