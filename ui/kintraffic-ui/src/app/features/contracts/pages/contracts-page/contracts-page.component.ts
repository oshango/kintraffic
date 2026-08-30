import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit } from '@angular/core';

import { Contract } from '../../models/contract.model';
import { ContractService } from '../../services/contract.service';

@Component({
  selector: 'app-contracts-page',
  standalone: true,
  imports: [CommonModule],
  template: `
    <section>
      <h2>Contracts</h2>
      <ul>
        <li *ngFor="let contract of contracts">
          {{ contract.vendorName }} - {{ contract.status }}
        </li>
      </ul>
    </section>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ContractsPageComponent implements OnInit {
  contracts: Contract[] = [];

  constructor(private readonly contractService: ContractService) {}

  ngOnInit(): void {
    this.contractService.getContracts().subscribe((contracts) => {
      this.contracts = contracts;
    });
  }
}
