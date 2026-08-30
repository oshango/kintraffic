using KinTraffic.Api.Models;

namespace KinTraffic.Api.Repositories.Contracts;

public class ContractRepository
{
    private readonly List<Contract> _contracts =
    [
        new Contract
        {
            Id = "contract-201",
            VendorName = "SignalWorks Ltd",
            ServiceType = "Preventive Maintenance",
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 12, 31),
            Status = "active"
        }
    ];

    public IEnumerable<Contract> GetContracts() => _contracts;
}
