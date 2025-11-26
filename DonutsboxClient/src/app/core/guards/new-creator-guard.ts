import { inject, PLATFORM_ID } from '@angular/core';
import { isPlatformServer } from '@angular/common';
import { CanActivateFn, Router } from '@angular/router';
import { SessionService } from '../services/session.service';
import { catchError, map, of } from 'rxjs';

export const newCreatorGuard: CanActivateFn = (route, state) => {
  const sessionService = inject(SessionService);
  const router = inject(Router);
  const platformId = inject(PLATFORM_ID);

  if (isPlatformServer(platformId)) {
    return of(true);
  }

  return sessionService.ensureSession().pipe(
    map((session) => {
      if (!session) {
        return router.createUrlTree(['/auth/login']);
      }

      if (session.isCreator && !session.hasCreatorPage) {
        return true;
      }

      if (session.isCreator && session.userId) {
        return router.createUrlTree(['/profile', session.userId]);
      } else {
        return router.createUrlTree(['/']);
      }
    }),
    catchError(() => {
      return of(router.createUrlTree(['/auth/login']));
    })
  );
};
