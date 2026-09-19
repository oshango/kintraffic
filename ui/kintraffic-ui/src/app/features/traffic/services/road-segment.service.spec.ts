import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { RoadSegmentService } from './road-segment.service';

describe('RoadSegmentService', () => {
  let service: RoadSegmentService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
    });

    service = TestBed.inject(RoadSegmentService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('gets all segments from the API', () => {
    service.getSegments().subscribe((segments) => expect(segments).toEqual([]));

    const request = httpMock.expectOne('/api/segments');
    expect(request.request.method).toBe('GET');
    request.flush([]);
  });

  it('gets one segment by id', () => {
    service.getSegment(1).subscribe((segment) => expect(segment.id).toBe(1));

    const request = httpMock.expectOne('/api/segments/1');
    expect(request.request.method).toBe('GET');
    request.flush({ id: 1, name: 'Segment 1', corridorName: null, geometry: '{}', lengthMeters: null });
  });

  it('posts a new segment', () => {
    const segment = {
      name: 'Segment 1',
      corridorName: 'Boulevard du 30 Juin',
      geometry: '{}',
      lengthMeters: 500,
    };

    service.addSegment(segment).subscribe((created) => expect(created.id).toBe(6));

    const request = httpMock.expectOne('/api/segments');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual(segment);
    request.flush({ id: 6, ...segment });
  });
});
