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
    public async Task<ActionResult<IEnumerable<TrafficSignalDto>>> GetSignals() =>
        Ok(await _service.GetSignalsAsync());

    [HttpGet("signals/{id}")]
    public async Task<ActionResult<TrafficSignalDto>> GetSignalById(string id)
    {
        var signal = await _service.GetSignalByIdAsync(id);
        return signal is null ? NotFound() : Ok(signal);
    }
}
