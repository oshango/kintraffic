import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { InventoryItem } from '../models/inventory.model';

@Injectable({ providedIn: 'root' })
export class InventoryService {
  constructor(private readonly http: HttpClient) {}

  getInventory(): Observable<InventoryItem[]> {
    return this.http.get<InventoryItem[]>('/api/inventory');
  }
}
