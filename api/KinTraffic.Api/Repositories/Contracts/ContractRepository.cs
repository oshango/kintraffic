using KinTraffic.Api.Models;
using KinTraffic.Api.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KinTraffic.Api.Repositories.Contracts;

public class ContractRepository(KinTrafficDbContext dbContext)
{
    public async Task<IReadOnlyList<Contract>> GetContractsAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Contracts
            .AsNoTracking()
            .OrderBy(contract => contract.StartDate)
            .ToListAsync(cancellationToken);
}
