import { HttpContextToken, HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, switchMap, throwError } from 'rxjs';
import { TokenRefreshService } from '../services/token-refresh.service';

const SKIP_REFRESH = new HttpContextToken<boolean>(() => false);
const AUTH_ENDPOINTS = ['/api/Auth/login', '/api/Auth/register', '/api/Auth/refresh'];

export const authRefreshInterceptor: HttpInterceptorFn = (req, next) => {
  const refreshService = inject(TokenRefreshService);

  const shouldSkip =
    req.context.get(SKIP_REFRESH) ||
    AUTH_ENDPOINTS.some((urlPart) => req.url.includes(urlPart));

  if (shouldSkip) {
    return next(req);
  }

  return next(req).pipe(
    catchError((error) => {
      if (!(error instanceof HttpErrorResponse) || error.status !== 401) {
        return throwError(() => error);
      }

      console.log('🔄 Interceptor: 401 detected, attempting token refresh for', req.url);

      return refreshService.refreshTokens().pipe(
        switchMap((refreshResponse) => {
          console.log('✅ Interceptor: Token refreshed successfully, retrying request', req.url);
          const retriedRequest = req.clone({
            context: req.context.set(SKIP_REFRESH, true),
          });
          return next(retriedRequest);
        }),
        catchError((refreshError) => {
          console.error('❌ Interceptor: Token refresh failed, redirecting to login', refreshError);
          refreshService.handleRefreshFailure();
          return throwError(() => error);
        })
      );
    })
  );
};


