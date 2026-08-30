import { Pipe, PipeTransform } from '@angular/core';

import { TrafficSignalStatus } from '../models/traffic.model';

@Pipe({
  name: 'statusLabel',
  standalone: true,
})
export class StatusPipe implements PipeTransform {
  transform(value: TrafficSignalStatus | null | undefined): string {
    const labels: Record<TrafficSignalStatus, string> = {
      normal: 'Normal',
      warning: 'Warning',
      critical: 'Critical',
      offline: 'Offline',
    };

    return value ? labels[value] : 'Unknown';
  }
}
