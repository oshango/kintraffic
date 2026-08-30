using KinTraffic.Api.DTOs.Traffic;
using KinTraffic.Api.Models;
using KinTraffic.Api.Repositories.Traffic;

namespace KinTraffic.Api.Services.Traffic;

public class TrafficService
{
    private readonly TrafficRepository _repository;

    public TrafficService(TrafficRepository repository)
    {
        _repository = repository;
    }

    public IEnumerable<TrafficSignalDto> GetSignals() =>
        _repository.GetSignals().Select(signal => new TrafficSignalDto
        {
            Id = signal.Id,
            Name = signal.Name,
            Status = signal.Status,
            CycleTime = signal.CycleTime,
            QueueLength = signal.QueueLength,
            UpdatedAt = signal.UpdatedAt
        });

    public TrafficSignalDto? GetSignalById(string id)
    {
        var signal = _repository.GetSignalById(id);
        if (signal is null)
        {
            return null;
        }

        return new TrafficSignalDto
        {
            Id = signal.Id,
            Name = signal.Name,
            Status = signal.Status,
            CycleTime = signal.CycleTime,
            QueueLength = signal.QueueLength,
            UpdatedAt = signal.UpdatedAt
        };
    }
}
