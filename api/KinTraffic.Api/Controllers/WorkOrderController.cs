using KinTraffic.Api.DTOs.WorkOrders;
using KinTraffic.Api.Services.WorkOrders;
using Microsoft.AspNetCore.Mvc;

namespace KinTraffic.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WorkOrderController : ControllerBase
{
    private readonly WorkOrderService _service;

    public WorkOrderController(WorkOrderService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<WorkOrderDto>>> GetWorkOrders(CancellationToken cancellationToken) =>
        Ok(await _service.GetWorkOrdersAsync(cancellationToken));
}
