import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit } from '@angular/core';

import { Invoice } from '../../models/invoice.model';
import { BillingService } from '../../services/billing.service';

@Component({
  selector: 'app-billing-page',
  standalone: true,
  imports: [CommonModule],
  template: `
    <section>
      <h2>Billing</h2>
      <ul>
        <li *ngFor="let invoice of invoices">
          {{ invoice.id }} - {{ invoice.status }}
        </li>
      </ul>
    </section>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BillingPageComponent implements OnInit {
  invoices: Invoice[] = [];

  constructor(private readonly billingService: BillingService) {}

  ngOnInit(): void {
    this.billingService.getInvoices().subscribe((invoices) => {
      this.invoices = invoices;
    });
  }
}
