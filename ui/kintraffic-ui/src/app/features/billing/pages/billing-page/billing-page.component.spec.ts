import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';

import { BillingService } from '../../services/billing.service';
import { BillingPageComponent } from './billing-page.component';

describe('BillingPageComponent', () => {
  let component: BillingPageComponent;
  let fixture: ComponentFixture<BillingPageComponent>;
  let billingService: { getInvoices: jest.Mock };

  beforeEach(async () => {
    billingService = {
      getInvoices: jest.fn().mockReturnValue(
        of([
          {
            id: 'inv-5001',
            contractId: 'contract-201',
            amount: 12500,
            issuedAt: '2026-08-01',
            status: 'issued',
          },
        ]),
      ),
    };

    await TestBed.configureTestingModule({
      imports: [BillingPageComponent],
      providers: [{ provide: BillingService, useValue: billingService }],
    }).compileComponents();

    fixture = TestBed.createComponent(BillingPageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should load invoices on init', () => {
    expect(billingService.getInvoices).toHaveBeenCalled();
    expect(component.invoices.length).toBe(1);
  });
});
