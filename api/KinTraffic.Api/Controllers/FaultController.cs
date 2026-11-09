using KinTraffic.Api.DTOs.Faults;
using KinTraffic.Api.Services.Faults;
using Microsoft.AspNetCore.Mvc;

namespace KinTraffic.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Route("api/faults")]
public class FaultController : ControllerBase
{
    private readonly FaultService _service;

    public FaultController(FaultService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<FaultDto>>> GetFaults(CancellationToken cancellationToken) =>
        Ok(await _service.GetFaultsAsync(cancellationToken));
}
