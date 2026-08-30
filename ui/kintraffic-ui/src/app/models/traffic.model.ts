export type TrafficSignalStatus = 'normal' | 'warning' | 'critical' | 'offline';

export interface TrafficSignal {
  id: string;
  name: string;
  status: TrafficSignalStatus;
  cycleTime: number;
  queueLength: number;
  updatedAt: string;
}
