using KinTraffic.Api.Models;
using KinTraffic.Api.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KinTraffic.Api.Repositories.Billing;

public class BillingRepository(KinTrafficDbContext dbContext)
{
    public async Task<IReadOnlyList<Invoice>> GetInvoicesAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Invoices
            .AsNoTracking()
            .OrderByDescending(invoice => invoice.IssuedAt)
            .ToListAsync(cancellationToken);
}
