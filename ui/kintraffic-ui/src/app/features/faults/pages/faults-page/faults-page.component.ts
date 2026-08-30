import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit } from '@angular/core';

import { Fault } from '../../models/fault.model';
import { FaultService } from '../../services/fault.service';

@Component({
  selector: 'app-faults-page',
  standalone: true,
  imports: [CommonModule],
  template: `
    <section>
      <h2>Faults</h2>
      <ul>
        <li *ngFor="let fault of faults">
          {{ fault.description }} ({{ fault.severity }})
        </li>
      </ul>
    </section>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FaultsPageComponent implements OnInit {
  faults: Fault[] = [];

  constructor(private readonly faultService: FaultService) {}

  ngOnInit(): void {
    this.faultService.getFaults().subscribe((faults) => {
      this.faults = faults;
    });
  }
}
