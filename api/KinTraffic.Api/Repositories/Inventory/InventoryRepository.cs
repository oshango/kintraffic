using KinTraffic.Api.Models;
using KinTraffic.Api.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KinTraffic.Api.Repositories.Inventory;

public class InventoryRepository(KinTrafficDbContext dbContext)
{
    public async Task<IReadOnlyList<InventoryItem>> GetInventoryAsync(CancellationToken cancellationToken = default) =>
        await dbContext.InventoryItems
            .AsNoTracking()
            .OrderBy(item => item.Name)
            .ToListAsync(cancellationToken);
}
