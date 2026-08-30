import { Routes } from '@angular/router';

import { HealthComponent } from './components/health/health.component';
import { AssetsPageComponent } from './features/assets/pages/assets-page/assets-page.component';
import { AuditPageComponent } from './features/audit/pages/audit-page/audit-page.component';
import { BillingPageComponent } from './features/billing/pages/billing-page/billing-page.component';
import { ContractsPageComponent } from './features/contracts/pages/contracts-page/contracts-page.component';
import { FaultsPageComponent } from './features/faults/pages/faults-page/faults-page.component';
import { InventoryPageComponent } from './features/inventory/pages/inventory-page/inventory-page.component';
import { LoginPageComponent } from './features/security/pages/login-page/login-page.component';
import { WorkOrdersPageComponent } from './features/work-orders/pages/work-orders-page/work-orders-page.component';
import { DashboardComponent } from './pages/dashboard/dashboard.component';

export const routes: Routes = [
  { path: '', component: DashboardComponent },
  { path: 'health', component: HealthComponent },
  { path: 'assets', component: AssetsPageComponent },
  { path: 'faults', component: FaultsPageComponent },
  { path: 'work-orders', component: WorkOrdersPageComponent },
  { path: 'inventory', component: InventoryPageComponent },
  { path: 'contracts', component: ContractsPageComponent },
  { path: 'billing', component: BillingPageComponent },
  { path: 'audit', component: AuditPageComponent },
  { path: 'login', component: LoginPageComponent },
  { path: '**', redirectTo: '' },
];
