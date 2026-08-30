export type WorkOrderStatus = 'queued' | 'assigned' | 'in-progress' | 'completed';

export interface WorkOrder {
  id: string;
  faultId: string;
  technicianId?: string;
  status: WorkOrderStatus;
  priority: 'low' | 'normal' | 'high' | 'urgent';
  scheduledFor: string;
}
