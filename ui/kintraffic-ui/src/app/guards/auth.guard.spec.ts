import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';

import { authGuard } from './auth.guard';

describe('authGuard', () => {
  let router: { createUrlTree: jest.Mock };

  beforeEach(async () => {
    router = {
      createUrlTree: jest.fn((commands, extras) => ({ commands, extras })),
    };

    await TestBed.configureTestingModule({
      providers: [{ provide: Router, useValue: router }],
    }).compileComponents();

    localStorage.clear();
  });

  it('should redirect to login when no token is present', () => {
    const result = TestBed.runInInjectionContext(() =>
      authGuard({ data: {} } as never, { url: '/dashboard' } as never),
    );

    expect(router.createUrlTree).toHaveBeenCalledWith(['/login'], {
      queryParams: { returnUrl: '/dashboard' },
    });
    expect(result).toEqual({ commands: ['/login'], extras: { queryParams: { returnUrl: '/dashboard' } } });
  });

  it('should allow access when the token is present and the role matches', () => {
    localStorage.setItem('token', 'abc123');
    localStorage.setItem('user', JSON.stringify({ roles: ['admin'] }));

    const result = TestBed.runInInjectionContext(() =>
      authGuard({ data: { role: 'admin' } } as never, { url: '/dashboard' } as never),
    );

    expect(result).toBe(true);
  });

  it('should redirect to unauthorized when the required role is missing', () => {
    localStorage.setItem('token', 'abc123');
    localStorage.setItem('user', JSON.stringify({ roles: ['user'] }));

    const result = TestBed.runInInjectionContext(() =>
      authGuard({ data: { role: 'admin' } } as never, { url: '/dashboard' } as never),
    );

    expect(router.createUrlTree).toHaveBeenCalledWith(['/unauthorized']);
    expect(result).toEqual({ commands: ['/unauthorized'], extras: undefined });
  });
});
