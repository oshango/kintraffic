using KinTraffic.Api.Models;

namespace KinTraffic.Api.Repositories.WorkOrders;

public class WorkOrderRepository
{
    private readonly List<WorkOrder> _workOrders =
    [
        new WorkOrder
        {
            Id = "wo-2001",
            FaultId = "fault-1001",
            TechnicianId = "tech-01",
            Status = "assigned",
            Priority = "high",
            ScheduledFor = new DateTime(2026, 8, 30, 10, 0, 0, DateTimeKind.Utc)
        }
    ];

    public IEnumerable<WorkOrder> GetWorkOrders() => _workOrders;
}
