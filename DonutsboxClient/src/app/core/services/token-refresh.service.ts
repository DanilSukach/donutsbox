import { inject, Injectable } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService, AuthResponseDto } from '@app/api/auth';
import { Observable, throwError, shareReplay, finalize, tap, catchError } from 'rxjs';
import { AuthTokenService } from './auth-token.service';
import { SessionService } from './session.service';

@Injectable({ providedIn: 'root' })
export class TokenRefreshService {
  private readonly authApi = inject(AuthService);
  private readonly tokens = inject(AuthTokenService);
  private readonly sessionService = inject(SessionService);
  private readonly router = inject(Router);

  private refreshRequest$: Observable<AuthResponseDto> | null = null;

  refreshTokens(): Observable<AuthResponseDto> {
    const refreshToken = this.tokens.getRefreshToken();
    if (!refreshToken) {
      console.error('❌ TokenRefreshService: Missing refresh token');
      return throwError(() => new Error('Missing refresh token'));
    }

    console.log('🔄 TokenRefreshService: Starting token refresh');

    if (!this.refreshRequest$) {
      this.refreshRequest$ = this.authApi
        .apiAuthRefreshPost({ refreshToken })
        .pipe(
          tap((response) => {
            console.log('✅ TokenRefreshService: Token refresh response received', {
              hasAccessToken: !!response?.accessToken,
              hasRefreshToken: !!response?.refreshToken
            });
            if (!response?.refreshToken) {
              throw new Error('Refresh response does not include refresh token');
            }
            this.tokens.setRefreshToken(response.refreshToken);
            console.log('✅ TokenRefreshService: Refresh token saved');
            // Access token должен быть установлен в cookie бэкендом через AppendAuthCookie
          }),
          catchError((error: unknown) => {
            console.error('❌ TokenRefreshService: Token refresh failed', error);
            return throwError(() => error);
          }),
          finalize(() => {
            this.refreshRequest$ = null;
          }),
          shareReplay(1)
        );
    }

    return this.refreshRequest$;
  }

  handleRefreshFailure(): void {
    this.tokens.clear();
    this.sessionService.clearSession();
    void this.router.navigate(['/auth/login']);
  }
}


