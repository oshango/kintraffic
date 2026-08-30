using KinTraffic.Api.Models;

namespace KinTraffic.Api.Repositories.Faults;

public class FaultRepository
{
    private readonly List<Fault> _faults =
    [
        new Fault
        {
            Id = "fault-1001",
            JunctionId = "junction-01",
            AssetId = "asset-11",
            Severity = "high",
            Status = "open",
            Description = "Signal controller overheating",
            CreatedAt = new DateTime(2026, 8, 30, 8, 10, 0, DateTimeKind.Utc)
        }
    ];

    public IEnumerable<Fault> GetFaults() => _faults;
}
