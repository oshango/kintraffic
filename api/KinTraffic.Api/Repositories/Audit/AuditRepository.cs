using KinTraffic.Api.Models;
using KinTraffic.Api.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KinTraffic.Api.Repositories.Audit;

public class AuditRepository(KinTrafficDbContext dbContext)
{
    public async Task<IReadOnlyList<AuditLog>> GetLogsAsync(CancellationToken cancellationToken = default) =>
        await dbContext.AuditLogs
            .AsNoTracking()
            .OrderByDescending(log => log.OccurredAt)
            .ToListAsync(cancellationToken);
}
