import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';

import { AuditService } from '../../services/audit.service';
import { AuditPageComponent } from './audit-page.component';

describe('AuditPageComponent', () => {
  let component: AuditPageComponent;
  let fixture: ComponentFixture<AuditPageComponent>;
  let auditService: { getAuditLogs: jest.Mock };

  beforeEach(async () => {
    auditService = {
      getAuditLogs: jest.fn().mockReturnValue(
        of([
          {
            id: 'audit-1',
            entity: 'SignalController',
            action: 'Updated',
            actor: 'admin@kintraffic.local',
            occurredAt: '2026-08-30T08:20:00Z',
          },
        ]),
      ),
    };

    await TestBed.configureTestingModule({
      imports: [AuditPageComponent],
      providers: [{ provide: AuditService, useValue: auditService }],
    }).compileComponents();

    fixture = TestBed.createComponent(AuditPageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should load audit logs on init', () => {
    expect(auditService.getAuditLogs).toHaveBeenCalled();
    expect(component.logs.length).toBe(1);
  });
});
