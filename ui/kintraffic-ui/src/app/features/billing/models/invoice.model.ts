export interface Invoice {
  id: string;
  contractId: string;
  amount: number;
  issuedAt: string;
  status: 'draft' | 'issued' | 'paid';
}
