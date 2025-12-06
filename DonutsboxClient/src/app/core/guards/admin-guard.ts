import { inject, PLATFORM_ID } from '@angular/core';
import { isPlatformServer } from '@angular/common';
import { CanActivateFn, Router } from '@angular/router';
import { SessionService } from '../services/session.service';
import { catchError, map, of } from 'rxjs';

export const adminGuard: CanActivateFn = (route, state) => {
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

      // Проверяем, является ли пользователь администратором
      const isAdmin = session.role === 'Administrator' || session.role === 'Admin';
      
      if (!isAdmin) {
        // Обычные пользователи не должны видеть страницу админа - редирект на 404
        return router.createUrlTree(['/404']);
      }

      return true;
    }),
    catchError((error) => {
      console.error('Ошибка проверки прав администратора:', error);
      return of(router.createUrlTree(['/auth/login']));
    })
  );
};

