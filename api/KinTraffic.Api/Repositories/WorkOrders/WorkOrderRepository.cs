using KinTraffic.Api.Models;
using KinTraffic.Api.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KinTraffic.Api.Repositories.WorkOrders;

public class WorkOrderRepository(KinTrafficDbContext dbContext)
{
    public async Task<IReadOnlyList<WorkOrder>> GetWorkOrdersAsync(CancellationToken cancellationToken = default) =>
        await dbContext.WorkOrders
            .AsNoTracking()
            .OrderBy(workOrder => workOrder.ScheduledFor)
            .ToListAsync(cancellationToken);
}
