using KinTraffic.Api.DTOs.Traffic;
using KinTraffic.Api.Services.Traffic;
using Microsoft.AspNetCore.Mvc;

namespace KinTraffic.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TrafficStatesController : ControllerBase
{
    private readonly TrafficStateService _service;

    public TrafficStatesController(TrafficStateService service) => _service = service;

    [HttpGet]
    public ActionResult<IEnumerable<TrafficStateDto>> GetTrafficStates() => Ok(_service.GetTrafficStates());
}