import { inject, Injectable } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService, AuthResponseDto } from '@app/api/auth';
import { Observable, throwError, shareReplay, finalize, tap } from 'rxjs';
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
      return throwError(() => new Error('Missing refresh token'));
    }

    if (!this.refreshRequest$) {
      this.refreshRequest$ = this.authApi
        .apiAuthRefreshPost({ refreshToken })
        .pipe(
          tap((response) => {
            if (!response?.refreshToken) {
              throw new Error('Refresh response does not include refresh token');
            }
            this.tokens.setRefreshToken(response.refreshToken);
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


