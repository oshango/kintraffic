import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';

export const authGuard: CanActivateFn = (route, state) => {
  const router = inject(Router);
  const token = localStorage.getItem('token');
  const requiredRole = route.data['role'] as string | undefined;

  if (!token) {
    return router.createUrlTree(['/login'], {
      queryParams: { returnUrl: state.url },
    });
  }

  if (requiredRole) {
    const user = JSON.parse(localStorage.getItem('user') ?? '{}');
    const roles = user?.roles ?? [];

    if (!roles.includes(requiredRole)) {
      return router.createUrlTree(['/unauthorized']);
    }
  }

  return true;
};
