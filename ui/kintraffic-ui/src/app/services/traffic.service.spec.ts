import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { TrafficService } from './traffic.service';

describe('TrafficService', () => {
  let service: TrafficService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
    });

    service = TestBed.inject(TrafficService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should return a list of traffic signals from the API', () => {
    const mockSignals = [
      {
        id: 'signal-001',
        name: 'Main Street & 1st Ave',
        status: 'normal',
        cycleTime: 42,
        queueLength: 12,
        updatedAt: '2026-08-30T08:00:00Z',
      },
    ];

    service.getSignals().subscribe((signals) => {
      expect(signals).toEqual(mockSignals);
    });

    const req = httpMock.expectOne('/api/traffic/signals');
    expect(req.request.method).toBe('GET');
    req.flush(mockSignals);
  });

  it('should fetch a signal by id', () => {
    const mockSignal = {
      id: 'signal-001',
      name: 'Main Street & 1st Ave',
      status: 'warning',
      cycleTime: 45,
      queueLength: 16,
      updatedAt: '2026-08-30T08:10:00Z',
    };

    service.getSignalById('signal-001').subscribe((signal) => {
      expect(signal).toEqual(mockSignal);
    });

    const req = httpMock.expectOne('/api/traffic/signals/signal-001');
    expect(req.request.method).toBe('GET');
    req.flush(mockSignal);
  });
});
