import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { RoadSegment } from '../models/road-segment.model';

@Injectable({ providedIn: 'root' })
export class RoadSegmentService {
  constructor(private readonly http: HttpClient) {}

  getSegments(): Observable<RoadSegment[]> {
    return this.http.get<RoadSegment[]>('/api/segments');
  }

  getSegment(id: number): Observable<RoadSegment> {
    return this.http.get<RoadSegment>(`/api/segments/${id}`);
  }

  addSegment(segment: Omit<RoadSegment, 'id'>): Observable<RoadSegment> {
    return this.http.post<RoadSegment>('/api/segments', segment);
  }
}
