using KinTraffic.Api.DTOs.Contracts;
using KinTraffic.Api.Services.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace KinTraffic.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ContractController : ControllerBase
{
    private readonly ContractService _service;

    public ContractController(ContractService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ContractDto>>> GetContracts(CancellationToken cancellationToken) =>
        Ok(await _service.GetContractsAsync(cancellationToken));
}
