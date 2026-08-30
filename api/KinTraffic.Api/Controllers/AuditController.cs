using KinTraffic.Api.DTOs.Audit;
using KinTraffic.Api.Services.Audit;
using Microsoft.AspNetCore.Mvc;

namespace KinTraffic.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuditController : ControllerBase
{
    private readonly AuditService _service;

    public AuditController(AuditService service)
    {
        _service = service;
    }

    [HttpGet("logs")]
    public ActionResult<IEnumerable<AuditLogDto>> GetLogs() => Ok(_service.GetLogs());
}
