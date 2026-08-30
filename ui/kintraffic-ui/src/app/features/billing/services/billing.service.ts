import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { Invoice } from '../models/invoice.model';

@Injectable({ providedIn: 'root' })
export class BillingService {
  constructor(private readonly http: HttpClient) {}

  getInvoices(): Observable<Invoice[]> {
    return this.http.get<Invoice[]>('/api/billing/invoices');
  }
}
