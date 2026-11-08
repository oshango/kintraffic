using KinTraffic.Api.DTOs.Traffic;
using KinTraffic.Api.Repositories.Traffic;

namespace KinTraffic.Api.Services.Traffic;

public class TrafficStateService
{
    private readonly TrafficStateRepository _repository;

    public TrafficStateService(TrafficStateRepository repository) => _repository = repository;

    public async Task<IReadOnlyList<TrafficStateDto>> GetTrafficStatesAsync(CancellationToken cancellationToken = default) =>
        (await _repository.GetTrafficStatesAsync(cancellationToken)).Select(state => new TrafficStateDto
        {
            Id = state.Id,
            RoadSegmentId = state.RoadSegmentId,
            CongestionLevel = state.CongestionLevel,
            AverageSpeedKph = state.AverageSpeedKph,
            VehicleCount = state.VehicleCount,
            RecordedAt = state.RecordedAt
        }).ToList();
}