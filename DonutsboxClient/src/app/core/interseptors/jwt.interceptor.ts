import { HttpErrorResponse, HttpRequest, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { TokenService } from '../services/token.service';
import { BehaviorSubject, catchError, filter, switchMap, take, throwError } from 'rxjs';
import { AuthRefresh } from '../services/auth-refresh';

const TOKEN_REFRESH_IN_PROGRESS$ = new BehaviorSubject<boolean>(false);
const TOKEN_STREAM$ = new BehaviorSubject<string | null>(null);
const AUTH_ENDPOINTS = ['/api/Auth/refresh', '/api/Auth/login', '/api/Auth/register'];

export const jwtInterceptor: HttpInterceptorFn = (req, next) => {
  const tokenService = inject(TokenService);
  const authRefreshService = inject(AuthRefresh);

  req = attachToken(req, tokenService.getAccessToken());

  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      const isAuthEndpoint = AUTH_ENDPOINTS.some(endpoint => req.url.includes(endpoint));

      if (error.status === 401 && !isAuthEndpoint) {
        if (!TOKEN_REFRESH_IN_PROGRESS$.value) {
          TOKEN_REFRESH_IN_PROGRESS$.next(true);
          TOKEN_STREAM$.next(null);

          return authRefreshService.refreshAccessToken().pipe(
            switchMap((response) => {
              TOKEN_REFRESH_IN_PROGRESS$.next(false);
              TOKEN_STREAM$.next(response.accessToken ?? null);

              const retriedRequest = attachToken(req, response.accessToken);
              return next(retriedRequest);
            }),
            catchError(refreshError => {
              TOKEN_REFRESH_IN_PROGRESS$.next(false);
              TOKEN_STREAM$.next(null);
              tokenService.clear();
              window.location.href = '/auth/login';
              return throwError(() => refreshError);
            })
          );
        }

        return TOKEN_STREAM$.pipe(
          filter((token): token is string => token !== null),
          take(1),
          switchMap(token => next(attachToken(req, token)))
        );
      }

      return throwError(() => error);
    })
  );
};

function attachToken(req: HttpRequest<any>, token: string | null): HttpRequest<any> {
  if (!token) {
    return req;
  }

  return req.clone({
    setHeaders: {
      Authorization: `Bearer ${token}`
    }
  });
}