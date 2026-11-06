using KinTraffic.Api.Models;

namespace KinTraffic.Api.Repositories.Traffic;

public class TrafficRepository : ITrafficRepository
{
    private readonly List<TrafficSignal> _signals =
    [
        new TrafficSignal
        {
            Id = "signal-001",
            Name = "Main Street & 1st Ave",
            Status = "normal",
            CycleTime = 42,
            QueueLength = 12,
            UpdatedAt = new DateTime(2026, 8, 30, 8, 0, 0, DateTimeKind.Utc)
        },
        new TrafficSignal
        {
            Id = "signal-002",
            Name = "Oak Blvd & Market Rd",
            Status = "warning",
            CycleTime = 58,
            QueueLength = 24,
            UpdatedAt = new DateTime(2026, 8, 30, 8, 3, 0, DateTimeKind.Utc)
        }
    ];

    public Task<IReadOnlyList<TrafficSignal>> GetSignalsAsync() =>
        Task.FromResult<IReadOnlyList<TrafficSignal>>(_signals);

    public Task<TrafficSignal?> GetSignalByIdAsync(string id) =>
        Task.FromResult(_signals.FirstOrDefault(signal => signal.Id == id));
}
