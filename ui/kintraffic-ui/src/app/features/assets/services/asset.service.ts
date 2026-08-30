import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { Asset } from '../models/asset.model';

@Injectable({ providedIn: 'root' })
export class AssetService {
  constructor(private readonly http: HttpClient) {}

  getAssets(): Observable<Asset[]> {
    return this.http.get<Asset[]>('/api/assets');
  }
}
