import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit } from '@angular/core';

import { TrafficSignalComponent } from '../../components/traffic-signal/traffic-signal.component';
import { TrafficSignal } from '../../models/traffic.model';
import { TrafficService } from '../../services/traffic.service';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, TrafficSignalComponent],
  template: `
    <section>
      <h1>Traffic Dashboard</h1>
      <div *ngIf="signals.length; else emptyState">
        <app-traffic-signal *ngFor="let signal of signals" [signal]="signal"></app-traffic-signal>
      </div>
      <ng-template #emptyState>
        <p>No signals are currently available.</p>
      </ng-template>
    </section>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DashboardComponent implements OnInit {
  signals: TrafficSignal[] = [];

  constructor(private readonly trafficService: TrafficService) {}

  ngOnInit(): void {
    this.trafficService.getSignals().subscribe((signals) => {
      this.signals = signals;
    });
  }
}
