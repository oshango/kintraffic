using KinTraffic.Api.DTOs.Inventory;
using KinTraffic.Api.Services.Inventory;
using Microsoft.AspNetCore.Mvc;

namespace KinTraffic.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InventoryController : ControllerBase
{
    private readonly InventoryService _service;

    public InventoryController(InventoryService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<InventoryItemDto>>> GetInventory(CancellationToken cancellationToken) =>
        Ok(await _service.GetInventoryAsync(cancellationToken));
}
