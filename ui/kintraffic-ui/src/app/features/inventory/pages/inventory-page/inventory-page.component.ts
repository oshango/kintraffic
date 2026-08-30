import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit } from '@angular/core';

import { InventoryItem } from '../../models/inventory.model';
import { InventoryService } from '../../services/inventory.service';

@Component({
  selector: 'app-inventory-page',
  standalone: true,
  imports: [CommonModule],
  template: `
    <section>
      <h2>Inventory</h2>
      <ul>
        <li *ngFor="let item of inventory">
          {{ item.name }} - {{ item.quantityOnHand }} in stock
        </li>
      </ul>
    </section>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class InventoryPageComponent implements OnInit {
  inventory: InventoryItem[] = [];

  constructor(private readonly inventoryService: InventoryService) {}

  ngOnInit(): void {
    this.inventoryService.getInventory().subscribe((inventory) => {
      this.inventory = inventory;
    });
  }
}
