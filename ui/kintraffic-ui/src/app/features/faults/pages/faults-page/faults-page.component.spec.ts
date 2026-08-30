import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';

import { FaultService } from '../../services/fault.service';
import { FaultsPageComponent } from './faults-page.component';

describe('FaultsPageComponent', () => {
  let component: FaultsPageComponent;
  let fixture: ComponentFixture<FaultsPageComponent>;
  let faultService: { getFaults: jest.Mock };

  beforeEach(async () => {
    faultService = {
      getFaults: jest.fn().mockReturnValue(
        of([
          {
            id: 'fault-1001',
            junctionId: 'junction-01',
            assetId: 'asset-11',
            severity: 'high',
            status: 'open',
            description: 'Signal controller overheating',
            createdAt: '2026-08-30T08:10:00Z',
          },
        ]),
      ),
    };

    await TestBed.configureTestingModule({
      imports: [FaultsPageComponent],
      providers: [{ provide: FaultService, useValue: faultService }],
    }).compileComponents();

    fixture = TestBed.createComponent(FaultsPageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should load faults on init', () => {
    expect(faultService.getFaults).toHaveBeenCalled();
    expect(component.faults.length).toBe(1);
  });
});
