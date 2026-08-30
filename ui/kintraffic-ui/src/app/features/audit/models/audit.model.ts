export interface AuditLog {
  id: string;
  entity: string;
  action: string;
  actor: string;
  occurredAt: string;
}
