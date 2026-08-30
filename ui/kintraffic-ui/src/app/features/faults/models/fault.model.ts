export type FaultSeverity = 'low' | 'medium' | 'high' | 'critical';
export type FaultStatus = 'open' | 'assigned' | 'in-progress' | 'resolved';

export interface Fault {
  id: string;
  junctionId: string;
  assetId: string;
  severity: FaultSeverity;
  status: FaultStatus;
  description: string;
  createdAt: string;
}
