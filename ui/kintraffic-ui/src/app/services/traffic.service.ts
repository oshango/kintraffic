import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { TrafficSignal } from '../models/traffic.model';

@Injectable({
  providedIn: 'root',
})
export class TrafficService {
  constructor(private readonly http: HttpClient) {}

  getSignals(): Observable<TrafficSignal[]> {
    return this.http.get<TrafficSignal[]>('/api/traffic/signals');
  }

  getSignalById(id: string): Observable<TrafficSignal> {
    return this.http.get<TrafficSignal>(`/api/traffic/signals/${id}`);
  }
}
