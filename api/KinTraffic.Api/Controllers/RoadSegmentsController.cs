using KinTraffic.Api.DTOs.Traffic;
using KinTraffic.Api.Services.Traffic;
using Microsoft.AspNetCore.Mvc;

namespace KinTraffic.Api.Controllers;

[ApiController]
[Route("api/segments")]
public class RoadSegmentsController : ControllerBase
{
    private readonly RoadSegmentService _service;

    public RoadSegmentsController(RoadSegmentService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<RoadSegmentDto>>> GetRoadSegments() =>
        Ok(await _service.GetAllAsync());

    [HttpGet("{id:int}")]
    public async Task<ActionResult<RoadSegmentDto>> GetRoadSegment(int id)
    {
        var segment = await _service.GetByIdAsync(id);
        return segment is null ? NotFound() : Ok(segment);
    }

    [HttpPost]
    public async Task<ActionResult<RoadSegmentDto>> AddRoadSegment(RoadSegmentDto dto)
    {
        var segment = await _service.AddAsync(dto);
        return CreatedAtAction(nameof(GetRoadSegment), new { id = segment.Id }, segment);
    }
}