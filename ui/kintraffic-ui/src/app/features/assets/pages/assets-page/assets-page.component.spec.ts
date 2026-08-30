import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';

import { AssetService } from '../../services/asset.service';
import { AssetsPageComponent } from './assets-page.component';

describe('AssetsPageComponent', () => {
  let component: AssetsPageComponent;
  let fixture: ComponentFixture<AssetsPageComponent>;
  let assetService: { getAssets: jest.Mock };

  beforeEach(async () => {
    assetService = {
      getAssets: jest.fn().mockReturnValue(
        of([
          {
            id: 'asset-11',
            junctionId: 'junction-01',
            name: 'Controller A',
            type: 'controller',
            status: 'online',
          },
        ]),
      ),
    };

    await TestBed.configureTestingModule({
      imports: [AssetsPageComponent],
      providers: [{ provide: AssetService, useValue: assetService }],
    }).compileComponents();

    fixture = TestBed.createComponent(AssetsPageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should load assets on init', () => {
    expect(assetService.getAssets).toHaveBeenCalled();
    expect(component.assets.length).toBe(1);
  });
});
