import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { AssetService } from './asset.service';

describe('AssetService', () => {
  let service: AssetService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
    });

    service = TestBed.inject(AssetService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should call the assets API endpoint', () => {
    service.getAssets().subscribe((assets) => {
      expect(assets).toEqual([]);
    });

    const req = httpMock.expectOne('/api/assets');
    expect(req.request.method).toBe('GET');
    req.flush([]);
  });
});
