import { inject, PLATFORM_ID } from '@angular/core';
import { isPlatformServer } from '@angular/common';
import { CanActivateFn, Router } from '@angular/router';
import { SessionService } from '../services/session.service';
import { catchError, map, of } from 'rxjs';

export const guestOnlyGuard: CanActivateFn = (route, state) => {
  const sessionService = inject(SessionService);
  const router = inject(Router);
  const platformId = inject(PLATFORM_ID);

  if (isPlatformServer(platformId)) {
    return of(true);
  }

  return sessionService.ensureSession().pipe(
    map((session) => {
      if (session) {
        // Если админ - редиректим на /management
        const isAdmin = session.role === 'Administrator' || session.role === 'Admin';
        if (isAdmin) {
          return router.createUrlTree(['/management']);
        }
        return router.createUrlTree(['/feed']);
      }
      return true;
    }),
    catchError(() => of(true))
  );
};


