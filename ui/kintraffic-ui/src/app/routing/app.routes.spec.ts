import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';

import { routes } from '../app.routes';

describe('App routes', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [RouterTestingHarness],
    });
  });

  it('should have a dashboard route', () => {
    expect(routes.some((route) => route.path === '')).toBeTruthy();
  });

  it('should have a health route', () => {
    expect(routes.some((route) => route.path === 'health')).toBeTruthy();
  });

  it('should redirect unmatched routes to the dashboard', () => {
    const fallback = routes.find((route) => route.path === '**');
    expect(fallback).toBeTruthy();
    expect(fallback?.redirectTo).toBe('');
  });
});
