using KinTraffic.Api.Models;

namespace KinTraffic.Api.Repositories.Traffic;

public interface ITrafficRepository
{
    Task<IReadOnlyList<TrafficSignal>> GetSignalsAsync();
    Task<TrafficSignal?> GetSignalByIdAsync(string id);
}