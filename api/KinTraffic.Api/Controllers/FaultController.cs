using KinTraffic.Api.DTOs.Faults;
using KinTraffic.Api.Services.Faults;
using Microsoft.AspNetCore.Mvc;

namespace KinTraffic.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FaultController : ControllerBase
{
    private readonly FaultService _service;

    public FaultController(FaultService service)
    {
        _service = service;
    }

    [HttpGet]
    public ActionResult<IEnumerable<FaultDto>> GetFaults() => Ok(_service.GetFaults());
}
