import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, Input } from '@angular/core';

import { TrafficSignal } from '../../models/traffic.model';
import { StatusPipe } from '../../pipes/status.pipe';

@Component({
  selector: 'app-traffic-signal',
  standalone: true,
  imports: [CommonModule, StatusPipe],
  template: `
    <article class="traffic-signal" *ngIf="signal; else emptyState">
      <h3>{{ signal.name }}</h3>
      <p>Status: <strong>{{ signal.status | statusLabel }}</strong></p>
      <p>Cycle time: {{ signal.cycleTime }}s</p>
      <p>Queue length: {{ signal.queueLength }}</p>
      <small>Updated: {{ signal.updatedAt | date: 'short' }}</small>
    </article>

    <ng-template #emptyState>
      <p>No traffic signal data available.</p>
    </ng-template>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TrafficSignalComponent {
  @Input() signal?: TrafficSignal;
}
