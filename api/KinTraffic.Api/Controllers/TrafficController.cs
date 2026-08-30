using KinTraffic.Api.DTOs.Traffic;
using KinTraffic.Api.Services.Traffic;
using Microsoft.AspNetCore.Mvc;

namespace KinTraffic.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TrafficController : ControllerBase
{
    private readonly TrafficService _service;

    public TrafficController(TrafficService service)
    {
        _service = service;
    }

    [HttpGet("signals")]
    public ActionResult<IEnumerable<TrafficSignalDto>> GetSignals() => Ok(_service.GetSignals());

    [HttpGet("signals/{id}")]
    public ActionResult<TrafficSignalDto> GetSignalById(string id)
    {
        var signal = _service.GetSignalById(id);
        return signal is null ? NotFound() : Ok(signal);
    }
}
