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

    public IEnumerable<FaultDto> GetFaults() =>
        _repository.GetFaults().Select(fault => new FaultDto
        {
            Id = fault.Id,
            JunctionId = fault.JunctionId,
            AssetId = fault.AssetId,
            Severity = fault.Severity,
            Status = fault.Status,
            Description = fault.Description,
            CreatedAt = fault.CreatedAt
        });
}
