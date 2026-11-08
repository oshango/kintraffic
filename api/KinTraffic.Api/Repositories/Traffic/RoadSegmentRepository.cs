using KinTraffic.Api.Models;
using KinTraffic.Api.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KinTraffic.Api.Repositories.Traffic;

public class RoadSegmentRepository(KinTrafficDbContext dbContext)
{
    public async Task<IReadOnlyList<RoadSegment>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await dbContext.RoadSegments
            .AsNoTracking()
            .OrderBy(segment => segment.Id)
            .ToListAsync(cancellationToken);

    public Task<RoadSegment?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        dbContext.RoadSegments.AsNoTracking().FirstOrDefaultAsync(segment => segment.Id == id, cancellationToken);

    public async Task<RoadSegment> AddAsync(RoadSegment segment, CancellationToken cancellationToken = default)
    {
        dbContext.RoadSegments.Add(segment);
        await dbContext.SaveChangesAsync(cancellationToken);
        return segment;
    }
}