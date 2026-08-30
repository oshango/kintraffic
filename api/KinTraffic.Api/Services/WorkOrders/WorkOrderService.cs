using KinTraffic.Api.DTOs.WorkOrders;
using KinTraffic.Api.Repositories.WorkOrders;

namespace KinTraffic.Api.Services.WorkOrders;

public class WorkOrderService
{
    private readonly WorkOrderRepository _repository;

    public WorkOrderService(WorkOrderRepository repository)
    {
        _repository = repository;
    }

    public IEnumerable<WorkOrderDto> GetWorkOrders() =>
        _repository.GetWorkOrders().Select(workOrder => new WorkOrderDto
        {
            Id = workOrder.Id,
            FaultId = workOrder.FaultId,
            TechnicianId = workOrder.TechnicianId,
            Status = workOrder.Status,
            Priority = workOrder.Priority,
            ScheduledFor = workOrder.ScheduledFor
        });
}
