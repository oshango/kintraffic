using KinTraffic.Api.DTOs.Billing;
using KinTraffic.Api.Services.Billing;
using Microsoft.AspNetCore.Mvc;

namespace KinTraffic.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BillingController : ControllerBase
{
    private readonly BillingService _service;

    public BillingController(BillingService service)
    {
        _service = service;
    }

    [HttpGet("invoices")]
    public ActionResult<IEnumerable<InvoiceDto>> GetInvoices() => Ok(_service.GetInvoices());
}
