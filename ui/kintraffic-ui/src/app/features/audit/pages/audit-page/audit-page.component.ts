import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit } from '@angular/core';

import { AuditLog } from '../../models/audit.model';
import { AuditService } from '../../services/audit.service';

@Component({
  selector: 'app-audit-page',
  standalone: true,
  imports: [CommonModule],
  template: `
    <section>
      <h2>Audit</h2>
      <ul>
        <li *ngFor="let log of logs">
          {{ log.action }} - {{ log.actor }}
        </li>
      </ul>
    </section>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AuditPageComponent implements OnInit {
  logs: AuditLog[] = [];

  constructor(private readonly auditService: AuditService) {}

  ngOnInit(): void {
    this.auditService.getAuditLogs().subscribe((logs) => {
      this.logs = logs;
    });
  }
}
