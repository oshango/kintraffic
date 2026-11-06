using KinTraffic.Api.DTOs.Traffic;
using KinTraffic.Api.Models;
using KinTraffic.Api.Repositories.Traffic;

namespace KinTraffic.Api.Services.Traffic;

public class TrafficService
{
    private readonly ITrafficRepository _repository;

    public TrafficService(ITrafficRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<TrafficSignalDto>> GetSignalsAsync() =>
        (await _repository.GetSignalsAsync()).Select(signal => new TrafficSignalDto
        {
            Id = signal.Id,
            Name = signal.Name,
            Status = signal.Status,
            CycleTime = signal.CycleTime,
            QueueLength = signal.QueueLength,
            UpdatedAt = signal.UpdatedAt
        });

    public async Task<TrafficSignalDto?> GetSignalByIdAsync(string id)
    {
        var signal = await _repository.GetSignalByIdAsync(id);
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
