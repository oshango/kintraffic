import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { Contract } from '../models/contract.model';

@Injectable({ providedIn: 'root' })
export class ContractService {
  constructor(private readonly http: HttpClient) {}

  getContracts(): Observable<Contract[]> {
    return this.http.get<Contract[]>('/api/contract');
  }
}
