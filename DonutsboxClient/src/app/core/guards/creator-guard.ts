import { inject, PLATFORM_ID } from '@angular/core';
import { isPlatformServer } from '@angular/common';
import { CanActivateFn, Router } from '@angular/router';
import { SessionService } from '../services/session.service';
import { catchError, map, of } from 'rxjs';

export const creatorGuard: CanActivateFn = (route, state) => {
  const sessionService = inject(SessionService);
  const router = inject(Router);
  const platformId = inject(PLATFORM_ID);

  if (isPlatformServer(platformId)) {
    return of(true);
  }

  return sessionService.ensureSession().pipe(
    map((session) => {
      if (session?.isCreator) {
        return true;
      }
      return router.createUrlTree(['/auth/login']);
    }),
    catchError(() => {
      return of(router.createUrlTree(['/auth/login']));
    })
  );
};
