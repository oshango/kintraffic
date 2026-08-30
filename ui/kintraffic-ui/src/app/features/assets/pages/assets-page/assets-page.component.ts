import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit } from '@angular/core';

import { Asset } from '../../models/asset.model';
import { AssetService } from '../../services/asset.service';

@Component({
  selector: 'app-assets-page',
  standalone: true,
  imports: [CommonModule],
  template: `
    <section>
      <h2>Assets</h2>
      <ul>
        <li *ngFor="let asset of assets">
          {{ asset.name }} - {{ asset.status }}
        </li>
      </ul>
    </section>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AssetsPageComponent implements OnInit {
  assets: Asset[] = [];

  constructor(private readonly assetService: AssetService) {}

  ngOnInit(): void {
    this.assetService.getAssets().subscribe((assets) => {
      this.assets = assets;
    });
  }
}
