export interface Contract {
  id: string;
  vendorName: string;
  serviceType: string;
  startDate: string;
  endDate: string;
  status: 'active' | 'expiring' | 'expired';
}
