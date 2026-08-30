import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';

import { WorkOrderService } from '../../services/work-order.service';
import { WorkOrdersPageComponent } from './work-orders-page.component';

describe('WorkOrdersPageComponent', () => {
  let component: WorkOrdersPageComponent;
  let fixture: ComponentFixture<WorkOrdersPageComponent>;
  let workOrderService: { getWorkOrders: jest.Mock };

  beforeEach(async () => {
    workOrderService = {
      getWorkOrders: jest.fn().mockReturnValue(
        of([
          {
            id: 'wo-2001',
            faultId: 'fault-1001',
            technicianId: 'tech-01',
            status: 'assigned',
            priority: 'high',
            scheduledFor: '2026-08-30T10:00:00Z',
          },
        ]),
      ),
    };

    await TestBed.configureTestingModule({
      imports: [WorkOrdersPageComponent],
      providers: [{ provide: WorkOrderService, useValue: workOrderService }],
    }).compileComponents();

    fixture = TestBed.createComponent(WorkOrdersPageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should load work orders on init', () => {
    expect(workOrderService.getWorkOrders).toHaveBeenCalled();
    expect(component.workOrders.length).toBe(1);
  });
});
