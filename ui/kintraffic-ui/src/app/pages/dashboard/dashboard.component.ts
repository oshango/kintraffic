import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';

import { TrafficSignal } from '../../models/traffic.model';
import { TrafficService } from '../../services/traffic.service';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <section class="dashboard">
      <header class="page-heading">
        <div>
          <p class="eyebrow">NETWORK OPERATIONS <span> / </span> API SNAPSHOT</p>
          <h1>Traffic overview</h1>
          <p class="subtitle">Signal status across monitored intersections.</p>
        </div>
        <div class="reporting-badge">
          <span class="reporting-dot" aria-hidden="true"></span>
          {{ loading ? 'CONNECTING' : error ? 'OFFLINE' : 'API CONNECTED' }}
        </div>
      </header>

      <div class="metric-grid" aria-label="Network summary">
        <article class="metric">
          <span class="metric-label">SIGNALS REPORTING</span>
          <strong>{{ signals.length | number: '2.0-0' }}</strong>
          <span class="metric-note">active intersections</span>
        </article>
        <article class="metric metric-attention">
          <span class="metric-label">NEEDS ATTENTION</span>
          <strong>{{ attentionCount | number: '2.0-0' }}</strong>
          <span class="metric-note">warning or critical status</span>
        </article>
        <article class="metric">
          <span class="metric-label">AVERAGE QUEUE</span>
          <strong>{{ averageQueue }}<small> veh</small></strong>
          <span class="metric-note">vehicles per intersection</span>
        </article>
        <article class="metric">
          <span class="metric-label">LONGEST QUEUE</span>
          <strong>{{ longestQueue }}<small> veh</small></strong>
          <span class="metric-note">current reported queue</span>
        </article>
      </div>

      <div *ngIf="error" class="notice notice-error" role="alert">
        <strong>Traffic data is unavailable.</strong>
        <span>Check the API connection and refresh the page.</span>
      </div>

      <section class="signals-section" aria-labelledby="signals-title">
        <div class="section-heading">
          <div>
            <p class="eyebrow">FIELD DEVICES</p>
            <h2 id="signals-title">Intersection signals</h2>
          </div>
          <a class="text-link" routerLink="/faults">Open fault log <span aria-hidden="true">→</span></a>
        </div>

        <div *ngIf="loading" class="table-message" role="status">Loading signal data…</div>
        <div *ngIf="!loading && !error && signals.length === 0" class="table-message">
          No intersection signals are reporting yet.
        </div>
        <div *ngIf="!loading && signals.length > 0" class="table-wrap">
          <table>
            <thead>
              <tr>
                <th scope="col">INTERSECTION</th>
                <th scope="col">STATUS</th>
                <th scope="col">QUEUE</th>
                <th scope="col">CYCLE</th>
                <th scope="col">LAST UPDATE</th>
              </tr>
            </thead>
            <tbody>
              <tr *ngFor="let signal of signals">
                <td class="intersection-name">{{ signal.name }}</td>
                <td>
                  <span class="status-pill" [ngClass]="'status-' + signal.status.toLowerCase()">
                    <span class="status-dot" aria-hidden="true"></span>
                    {{ signal.status | titlecase }}
                  </span>
                </td>
                <td class="numeric-cell">{{ signal.queueLength }} <span>veh</span></td>
                <td class="numeric-cell">{{ signal.cycleTime }} <span>sec</span></td>
                <td class="time-cell">{{ signal.updatedAt | date: 'MMM d, HH:mm' }}</td>
              </tr>
            </tbody>
          </table>
        </div>
      </section>

      <footer class="data-note">
        <span>DATA SOURCE</span>
        <span>Seeded sample rows; persistence depends on API database configuration</span>
        <span class="data-note-divider"></span>
        <span>Device timestamps shown per intersection</span>
      </footer>
    </section>
  `,
  styles: [`
    :host { display: block; }
    .dashboard { max-width: 1440px; margin: 0 auto; padding: 38px 36px 26px; }
    .page-heading, .section-heading { display: flex; align-items: flex-end; justify-content: space-between; gap: 20px; }
    .eyebrow { margin: 0 0 10px; color: #78857f; font-size: 10px; font-weight: 750; letter-spacing: 1.25px; }
    .eyebrow span { padding: 0 5px; color: #b6c0b9; }
    h1, h2 { margin: 0; color: #1d2c27; font-family: Georgia, "Times New Roman", serif; font-weight: 500; letter-spacing: 0; }
    h1 { font-size: 34px; }
    h2 { font-size: 21px; }
    .subtitle { margin: 8px 0 0; color: #68756f; font-size: 13px; }
    .reporting-badge { display: inline-flex; align-items: center; gap: 8px; padding: 8px 10px; border: 1px solid #dfe6dc; color: #53625a; background: #fff; font-size: 9px; font-weight: 750; letter-spacing: 0.75px; white-space: nowrap; }
    .reporting-dot, .status-dot { width: 7px; aspect-ratio: 1; border-radius: 50%; background: #4b9b70; }
    .metric-grid { display: grid; grid-template-columns: repeat(4, minmax(0, 1fr)); margin-top: 30px; border: 1px solid #dfe5dc; background: #fff; }
    .metric { display: flex; min-height: 132px; flex-direction: column; justify-content: center; padding: 19px 22px; border-right: 1px solid #e6eae4; }
    .metric:last-child { border-right: 0; }
    .metric-label { color: #75817b; font-size: 9px; font-weight: 750; letter-spacing: 0.8px; }
    .metric strong { margin-top: 10px; color: #1d2c27; font-family: Georgia, "Times New Roman", serif; font-size: 34px; font-weight: 500; line-height: 1; font-variant-numeric: tabular-nums; }
    .metric strong small { font-family: "Avenir Next", Avenir, "Segoe UI", sans-serif; font-size: 12px; font-weight: 600; }
    .metric-note { margin-top: 8px; color: #89948e; font-size: 11px; }
    .metric-attention strong { color: #bb553f; }
    .notice { display: flex; flex-wrap: wrap; gap: 6px 12px; margin-top: 18px; padding: 13px 15px; border-left: 3px solid #c76b52; color: #76402f; background: #fff2ed; font-size: 12px; }
    .signals-section { margin-top: 34px; }
    .section-heading { margin-bottom: 15px; align-items: flex-end; }
    .section-heading .eyebrow { margin-bottom: 6px; }
    .text-link { color: #366e5b; font-size: 12px; font-weight: 650; text-decoration: none; }
    .text-link:hover { color: #1d493a; text-decoration: underline; }
    .table-wrap { overflow-x: auto; border: 1px solid #dfe5dc; background: #fff; }
    table { width: 100%; min-width: 680px; border-collapse: collapse; text-align: left; }
    th { height: 40px; padding: 0 16px; border-bottom: 1px solid #e5e9e3; color: #78857f; font-size: 9px; font-weight: 750; letter-spacing: 0.75px; white-space: nowrap; }
    td { height: 59px; padding: 0 16px; border-bottom: 1px solid #edf0eb; color: #435149; font-size: 12px; white-space: nowrap; }
    tbody tr:last-child td { border-bottom: 0; }
    tbody tr:hover { background: #f8faf6; }
    .intersection-name { color: #263a31; font-weight: 650; }
    .status-pill { display: inline-flex; align-items: center; gap: 7px; padding: 5px 8px; color: #32724e; background: #edf6ef; font-size: 10px; font-weight: 650; }
    .status-warning { color: #956118; background: #fbf3e5; }
    .status-warning .status-dot { background: #d29a3d; }
    .status-critical { color: #a63e35; background: #fff0ed; }
    .status-critical .status-dot { background: #c65a4e; }
    .numeric-cell { color: #2e3e35; font-variant-numeric: tabular-nums; }
    .numeric-cell span { color: #89948e; font-size: 10px; }
    .time-cell { color: #69776f; font-variant-numeric: tabular-nums; }
    .table-message { padding: 34px 18px; border: 1px solid #dfe5dc; color: #68756f; background: #fff; font-size: 13px; }
    .data-note { display: flex; flex-wrap: wrap; align-items: center; gap: 10px; margin-top: 15px; color: #7e8983; font-size: 10px; }
    .data-note span:first-child { color: #66746d; font-size: 9px; font-weight: 750; letter-spacing: 0.7px; }
    .data-note-divider { width: 1px; height: 12px; background: #d6ddd5; }
    @media (max-width: 960px) { .dashboard { padding-inline: 24px; } .metric-grid { grid-template-columns: repeat(2, minmax(0, 1fr)); } .metric:nth-child(2) { border-right: 0; } .metric:nth-child(-n + 2) { border-bottom: 1px solid #e6eae4; } }
    @media (max-width: 600px) { .dashboard { padding: 26px 15px 20px; } .page-heading { align-items: flex-start; flex-direction: column; gap: 14px; } h1 { font-size: 29px; } .metric { min-height: 116px; padding: 16px 13px; } .metric strong { font-size: 29px; } .section-heading { align-items: flex-start; flex-direction: column; gap: 10px; } }
  `],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DashboardComponent implements OnInit {
  signals: TrafficSignal[] = [];
  loading = true;
  error = false;

  constructor(
    private readonly trafficService: TrafficService,
    private readonly changeDetector: ChangeDetectorRef,
  ) {}

  get attentionCount(): number {
    return this.signals.filter((signal) => ['warning', 'critical'].includes(signal.status.toLowerCase())).length;
  }

  get averageQueue(): number {
    if (this.signals.length === 0) return 0;
    return Math.round(this.signals.reduce((total, signal) => total + signal.queueLength, 0) / this.signals.length);
  }

  get longestQueue(): number {
    return Math.max(0, ...this.signals.map((signal) => signal.queueLength));
  }

  ngOnInit(): void {
    this.trafficService.getSignals().subscribe((signals) => {
      this.signals = signals;
      this.loading = false;
      this.changeDetector.markForCheck();
    }, () => {
      this.error = true;
      this.loading = false;
      this.changeDetector.markForCheck();
    });
  }
}
