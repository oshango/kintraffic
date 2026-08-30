using KinTraffic.Api.DTOs.Contracts;
using KinTraffic.Api.Repositories.Contracts;

namespace KinTraffic.Api.Services.Contracts;

public class ContractService
{
    private readonly ContractRepository _repository;

    public ContractService(ContractRepository repository)
    {
        _repository = repository;
    }

    public IEnumerable<ContractDto> GetContracts() =>
        _repository.GetContracts().Select(contract => new ContractDto
        {
            Id = contract.Id,
            VendorName = contract.VendorName,
            ServiceType = contract.ServiceType,
            StartDate = contract.StartDate,
            EndDate = contract.EndDate,
            Status = contract.Status
        });
}
