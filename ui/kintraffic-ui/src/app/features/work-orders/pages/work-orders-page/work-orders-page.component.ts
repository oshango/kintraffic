import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit } from '@angular/core';

import { WorkOrder } from '../../models/work-order.model';
import { WorkOrderService } from '../../services/work-order.service';

@Component({
  selector: 'app-work-orders-page',
  standalone: true,
  imports: [CommonModule],
  template: `
    <section>
      <h2>Work Orders</h2>
      <ul>
        <li *ngFor="let workOrder of workOrders">
          {{ workOrder.id }} - {{ workOrder.status }}
        </li>
      </ul>
    </section>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WorkOrdersPageComponent implements OnInit {
  workOrders: WorkOrder[] = [];

  constructor(private readonly workOrderService: WorkOrderService) {}

  ngOnInit(): void {
    this.workOrderService.getWorkOrders().subscribe((workOrders) => {
      this.workOrders = workOrders;
    });
  }
}
