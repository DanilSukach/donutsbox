import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { TokenService } from '../services/token.service';
import { BehaviorSubject, catchError, filter, switchMap, take, throwError } from 'rxjs';
import { AuthRefresh } from '../services/auth-refresh';

let refreshInProgress = new BehaviorSubject<boolean>(false);

export const jwtInterceptor: HttpInterceptorFn = (req, next) => {
  const tokenService = inject(TokenService);
  const authRefreshService = inject(AuthRefresh);

  const accessToken = tokenService.getAccessToken();

  if (accessToken) {
    req = req.clone({
      setHeaders: { Authorization: `Bearer ${accessToken}` }
    });
  }

  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      const authEndpoints = ['/api/Auth/refresh', '/api/Auth/login', '/api/Auth/register'];
      const isAuthEndpoint = authEndpoints.some(endpoint => req.url.includes(endpoint));

      if (error.status === 401 && !isAuthEndpoint) {
        console.warn('⚠️ [Interceptor] 401 Unauthorized - attempting token refresh...');

        if (refreshInProgress.value) {
          return refreshInProgress.pipe(
            filter(isRefreshing => !isRefreshing),
            take(1),
            switchMap(() => {
              const newToken = tokenService.getAccessToken();
              if (newToken) {
                const retryReq = req.clone({
                  setHeaders: { Authorization: `Bearer ${newToken}` }
                });
                return next(retryReq);
              }
              return throwError(() => new Error('No token after refresh'));
            })
          );
        }

        refreshInProgress.next(true);

        return authRefreshService.refreshAccessToken().pipe(
          switchMap(() => {
            const newToken = tokenService.getAccessToken();
            refreshInProgress.next(false);

            if (newToken) {
              const retryReq = req.clone({
                setHeaders: { Authorization: `Bearer ${newToken}` }
              });
              return next(retryReq);
            }
            return throwError(() => new Error('No token after refresh'));
          }),
          catchError(err => {
            console.error('❌ [Interceptor] Token refresh failed');
            refreshInProgress.next(false);
            tokenService.clear();
            window.location.href = '/auth/login';
            return throwError(() => err);
          })
        );
      }

      return throwError(() => error);
    })
  );
};