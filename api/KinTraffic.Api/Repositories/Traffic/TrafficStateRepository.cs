using KinTraffic.Api.Models;

namespace KinTraffic.Api.Repositories.Traffic;

public class TrafficStateRepository
{
    private readonly List<TrafficState> _states =
    [
        new TrafficState
        {
            Id = 1,
            RoadSegmentId = 1,
            CongestionLevel = "normal",
            AverageSpeedKph = 43.5m,
            VehicleCount = 18,
            RecordedAt = new DateTime(2026, 8, 30, 8, 5, 0, DateTimeKind.Utc)
        }
    ];

    public IEnumerable<TrafficState> GetTrafficStates() => _states;
}