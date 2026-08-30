import { ComponentFixture, TestBed } from '@angular/core/testing';

import { TrafficSignalComponent } from './traffic-signal.component';

describe('TrafficSignalComponent', () => {
  let component: TrafficSignalComponent;
  let fixture: ComponentFixture<TrafficSignalComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TrafficSignalComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(TrafficSignalComponent);
    component = fixture.componentInstance;
    component.signal = {
      id: 'signal-1',
      name: 'Main & 1st',
      status: 'normal',
      cycleTime: 45,
      queueLength: 14,
      updatedAt: '2026-08-30T08:00:00Z',
    };
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should render signal name and status', () => {
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Main & 1st');
    expect(compiled.textContent).toContain('Normal');
  });
});
