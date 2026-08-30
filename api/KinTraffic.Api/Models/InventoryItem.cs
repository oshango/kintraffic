namespace KinTraffic.Api.Models;

public class InventoryItem
{
    public string Id { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int QuantityOnHand { get; set; }
    public string Location { get; set; } = string.Empty;
    public int ReorderLevel { get; set; }
}
