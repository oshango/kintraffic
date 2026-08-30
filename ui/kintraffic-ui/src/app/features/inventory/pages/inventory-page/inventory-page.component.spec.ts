import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';

import { InventoryService } from '../../services/inventory.service';
import { InventoryPageComponent } from './inventory-page.component';

describe('InventoryPageComponent', () => {
  let component: InventoryPageComponent;
  let fixture: ComponentFixture<InventoryPageComponent>;
  let inventoryService: { getInventory: jest.Mock };

  beforeEach(async () => {
    inventoryService = {
      getInventory: jest.fn().mockReturnValue(
        of([
          {
            id: 'inv-100',
            sku: 'SP-LED-12',
            name: 'Signal LED Module',
            quantityOnHand: 18,
            location: 'Warehouse A',
            reorderLevel: 8,
          },
        ]),
      ),
    };

    await TestBed.configureTestingModule({
      imports: [InventoryPageComponent],
      providers: [{ provide: InventoryService, useValue: inventoryService }],
    }).compileComponents();

    fixture = TestBed.createComponent(InventoryPageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should load inventory on init', () => {
    expect(inventoryService.getInventory).toHaveBeenCalled();
    expect(component.inventory.length).toBe(1);
  });
});
