using KinTraffic.Api.Models;

namespace KinTraffic.Api.Repositories.Audit;

public class AuditRepository
{
    private readonly List<AuditLog> _logs =
    [
        new AuditLog
        {
            Id = "audit-1",
            Entity = "SignalController",
            Action = "Updated",
            Actor = "admin@kintraffic.local",
            OccurredAt = new DateTime(2026, 8, 30, 8, 20, 0, DateTimeKind.Utc)
        }
    ];

    public IEnumerable<AuditLog> GetLogs() => _logs;
}
