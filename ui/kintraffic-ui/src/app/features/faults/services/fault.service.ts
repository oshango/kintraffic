import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { Fault } from '../models/fault.model';

@Injectable({ providedIn: 'root' })
export class FaultService {
  constructor(private readonly http: HttpClient) {}

  getFaults(): Observable<Fault[]> {
    return this.http.get<Fault[]>('/api/faults');
  }
}
