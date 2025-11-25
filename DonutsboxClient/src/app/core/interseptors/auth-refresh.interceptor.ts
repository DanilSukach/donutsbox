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

      return refreshService.refreshTokens().pipe(
        switchMap(() => {
          const retriedRequest = req.clone({
            context: req.context.set(SKIP_REFRESH, true),
          });
          return next(retriedRequest);
        }),
        catchError(() => {
          refreshService.handleRefreshFailure();
          return throwError(() => error);
        })
      );
    })
  );
};


