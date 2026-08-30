import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';

import { ContractService } from '../../services/contract.service';
import { ContractsPageComponent } from './contracts-page.component';

describe('ContractsPageComponent', () => {
  let component: ContractsPageComponent;
  let fixture: ComponentFixture<ContractsPageComponent>;
  let contractService: { getContracts: jest.Mock };

  beforeEach(async () => {
    contractService = {
      getContracts: jest.fn().mockReturnValue(
        of([
          {
            id: 'contract-201',
            vendorName: 'SignalWorks Ltd',
            serviceType: 'Preventive Maintenance',
            startDate: '2026-01-01',
            endDate: '2026-12-31',
            status: 'active',
          },
        ]),
      ),
    };

    await TestBed.configureTestingModule({
      imports: [ContractsPageComponent],
      providers: [{ provide: ContractService, useValue: contractService }],
    }).compileComponents();

    fixture = TestBed.createComponent(ContractsPageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should load contracts on init', () => {
    expect(contractService.getContracts).toHaveBeenCalled();
    expect(component.contracts.length).toBe(1);
  });
});
