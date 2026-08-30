export interface InventoryItem {
  id: string;
  sku: string;
  name: string;
  quantityOnHand: number;
  location: string;
  reorderLevel: number;
}
