using KinTraffic.Api.DTOs.Assets;
using KinTraffic.Api.Services.Assets;
using Microsoft.AspNetCore.Mvc;

namespace KinTraffic.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AssetController : ControllerBase
{
    private readonly AssetService _service;

    public AssetController(AssetService service)
    {
        _service = service;
    }

    [HttpGet]
    public ActionResult<IEnumerable<AssetDto>> GetAssets() => Ok(_service.GetAssets());
}
