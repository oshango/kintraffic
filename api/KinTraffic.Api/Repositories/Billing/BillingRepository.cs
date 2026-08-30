using KinTraffic.Api.Models;

namespace KinTraffic.Api.Repositories.Billing;

public class BillingRepository
{
    private readonly List<Invoice> _invoices =
    [
        new Invoice
        {
            Id = "inv-5001",
            ContractId = "contract-201",
            Amount = 12500.00m,
            IssuedAt = new DateTime(2026, 8, 1),
            Status = "issued"
        }
    ];

    public IEnumerable<Invoice> GetInvoices() => _invoices;
}
