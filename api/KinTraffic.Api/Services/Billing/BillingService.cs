using KinTraffic.Api.DTOs.Billing;
using KinTraffic.Api.Repositories.Billing;

namespace KinTraffic.Api.Services.Billing;

public class BillingService
{
    private readonly BillingRepository _repository;

    public BillingService(BillingRepository repository)
    {
        _repository = repository;
    }

    public IEnumerable<InvoiceDto> GetInvoices() =>
        _repository.GetInvoices().Select(invoice => new InvoiceDto
        {
            Id = invoice.Id,
            ContractId = invoice.ContractId,
            Amount = invoice.Amount,
            IssuedAt = invoice.IssuedAt,
            Status = invoice.Status
        });
}
