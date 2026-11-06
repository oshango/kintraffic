import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';

import { TrafficService } from '../../services/traffic.service';
import { DashboardComponent } from './dashboard.component';

describe('DashboardComponent', () => {
  let component: DashboardComponent;
  let fixture: ComponentFixture<DashboardComponent>;
  let trafficService: { getSignals: jest.Mock };

  beforeEach(async () => {
    trafficService = {
      getSignals: jest.fn().mockReturnValue(
        of([
          {
            id: 'signal-1',
            name: 'Main & 1st',
            status: 'normal',
            cycleTime: 45,
            queueLength: 14,
            updatedAt: '2026-08-30T08:00:00Z',
          },
        ]),
      ),
    };

    await TestBed.configureTestingModule({
      imports: [DashboardComponent],
      providers: [provideRouter([]), { provide: TrafficService, useValue: trafficService }],
    }).compileComponents();

    fixture = TestBed.createComponent(DashboardComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should load signals on init', () => {
    expect(trafficService.getSignals).toHaveBeenCalled();
    expect(component.signals.length).toBe(1);
    expect(component.attentionCount).toBe(0);
    expect(component.averageQueue).toBe(14);
    expect(component.longestQueue).toBe(14);
    expect(fixture.nativeElement.textContent).toContain('Intersection signals');
  });
});
