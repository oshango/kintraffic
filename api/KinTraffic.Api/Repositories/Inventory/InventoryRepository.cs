using KinTraffic.Api.Models;

namespace KinTraffic.Api.Repositories.Inventory;

public class InventoryRepository
{
    private readonly List<InventoryItem> _inventory =
    [
        new InventoryItem
        {
            Id = "inv-100",
            Sku = "SP-LED-12",
            Name = "Signal LED Module",
            QuantityOnHand = 18,
            Location = "Warehouse A",
            ReorderLevel = 8
        }
    ];

    public IEnumerable<InventoryItem> GetInventory() => _inventory;
}
