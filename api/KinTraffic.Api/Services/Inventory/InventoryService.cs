using KinTraffic.Api.DTOs.Inventory;
using KinTraffic.Api.Repositories.Inventory;

namespace KinTraffic.Api.Services.Inventory;

public class InventoryService
{
    private readonly InventoryRepository _repository;

    public InventoryService(InventoryRepository repository)
    {
        _repository = repository;
    }

    public IEnumerable<InventoryItemDto> GetInventory() =>
        _repository.GetInventory().Select(item => new InventoryItemDto
        {
            Id = item.Id,
            Sku = item.Sku,
            Name = item.Name,
            QuantityOnHand = item.QuantityOnHand,
            Location = item.Location,
            ReorderLevel = item.ReorderLevel
        });
}
