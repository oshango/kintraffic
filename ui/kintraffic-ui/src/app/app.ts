import { Component, signal } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import {
  LucideActivity,
  LucideAlertTriangle,
  LucideArrowUpRight,
  LucideClipboardList,
  LucideFileSignature,
  LucideLayoutDashboard,
  LucidePackage,
  LucidePanelLeftClose,
  LucidePanelLeftOpen,
  LucideReceiptText,
  LucideScrollText,
  LucideTrafficCone,
} from '@lucide/angular';

@Component({
  imports: [
    RouterLink,
    RouterLinkActive,
    RouterOutlet,
    LucideActivity,
    LucideAlertTriangle,
    LucideArrowUpRight,
    LucideClipboardList,
    LucideFileSignature,
    LucideLayoutDashboard,
    LucidePackage,
    LucidePanelLeftClose,
    LucidePanelLeftOpen,
    LucideReceiptText,
    LucideScrollText,
    LucideTrafficCone,
  ],
  selector: 'app-root',
  templateUrl: './app.html',
})
export class App {
  readonly isSidebarCollapsed = signal(false);

  toggleSidebar(): void {
    this.isSidebarCollapsed.update((collapsed) => !collapsed);
  }
}
