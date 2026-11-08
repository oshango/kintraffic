using KinTraffic.Api.DTOs.Faults;
using KinTraffic.Api.Repositories.Faults;

namespace KinTraffic.Api.Services.Faults;

public class FaultService
{
    private readonly FaultRepository _repository;

    public FaultService(FaultRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<FaultDto>> GetFaultsAsync(CancellationToken cancellationToken = default) =>
        (await _repository.GetFaultsAsync(cancellationToken)).Select(fault => new FaultDto
        {
            Id = fault.Id,
            JunctionId = fault.Device.JunctionId,
            AssetId = fault.DeviceId,
            Severity = fault.Severity,
            Status = fault.Status,
            Description = fault.Description,
            CreatedAt = fault.CreatedAt
        }).ToList();
}
