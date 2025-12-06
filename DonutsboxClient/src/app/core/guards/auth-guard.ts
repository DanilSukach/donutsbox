import { inject, PLATFORM_ID } from '@angular/core';
import { isPlatformServer } from '@angular/common';
import { CanActivateFn, Router } from '@angular/router';
import { SessionService } from '../services/session.service';
import { catchError, map, of } from 'rxjs';

export const authGuard: CanActivateFn = (route, state) => {
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

      // Если админ - редиректим на /management (админу доступен только этот путь)
      // Но не перенаправляем, если уже на пути /management
      const isAdmin = session.role === 'Administrator' || session.role === 'Admin';
      const isManagementPath = state.url.startsWith('/management');
      
      if (isAdmin && !isManagementPath) {
        return router.createUrlTree(['/management']);
      }

      // Если админ пытается попасть на /management - разрешаем (adminGuard проверит права)
      if (isAdmin && isManagementPath) {
        return true;
      }

      // Обычные пользователи не должны попадать на /management
      if (!isAdmin && isManagementPath) {
        return router.createUrlTree(['/404']);
      }

      if (session.isCreator && !session.hasCreatorPage) {
        return router.createUrlTree(['/profile/setup']);
      }

      return true;
    }),
    catchError(() => {
      return of(router.createUrlTree(['/auth/login']));
    })
  );
};
