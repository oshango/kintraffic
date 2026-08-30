export interface Asset {
  id: string;
  junctionId: string;
  name: string;
  type: 'controller' | 'sensor' | 'camera' | 'detector';
  status: 'online' | 'offline' | 'maintenance';
}
