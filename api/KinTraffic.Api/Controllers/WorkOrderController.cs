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
    public ActionResult<IEnumerable<WorkOrderDto>> GetWorkOrders() => Ok(_service.GetWorkOrders());
}
