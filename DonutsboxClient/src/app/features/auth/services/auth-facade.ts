import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Router } from '@angular/router';
import { AuthResponseDto, AuthService, LoginRequestDto, RegisterRequestDto } from '@app/api/auth';
import { map, tap, take, switchMap, catchError, of, delay } from 'rxjs';
import { SessionService } from '@app/core/services/session.service';
import { AuthTokenService } from '@app/core/services/auth-token.service';


@Injectable({
  providedIn: 'root'
})
export class AuthFacade {
  private readonly authApiService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly sessionService = inject(SessionService);
  private readonly http = inject(HttpClient);
  private readonly tokenStorage = inject(AuthTokenService);

  register(registerData: RegisterRequestDto) {
    return this.authApiService.apiAuthRegisterPost(registerData).pipe(
      tap(() => {
        this.router.navigate(['/auth/login']);
      })
    );
  }

  login(loginData: LoginRequestDto) {
    return this.authApiService.apiAuthLoginPost(loginData).pipe(
      tap((resp: AuthResponseDto) => {
        this.tokenStorage.setRefreshToken(resp.refreshToken ?? null);
      }),
      // Небольшая задержка, чтобы cookie успели установиться
      delay(100),
      switchMap((resp: AuthResponseDto) =>
        this.sessionService.refreshSession().pipe(
          catchError((error) => {
            console.error('Ошибка при обновлении сессии после логина:', error);
            // Продолжаем даже если refreshSession не удался - используем данные из ответа логина
            return of(null);
          }),
          map(() => resp)
        )
      ),
      map((resp: AuthResponseDto) => {
        const guid = resp.userId ?? null;
        const isNewCreator = resp.isNewCreator ?? false;
        // Проверяем isFirstLogin, если поле существует
        const isFirstLogin = (resp as any).isFirstLogin ?? false;
        return { guid, isNewCreator, isFirstLogin };
      })
    );
  }

  logout(): void {
    const baseUrl = this.authApiService.configuration.basePath ?? '';
    this.tokenStorage.clear();

    this.http.post(`${baseUrl}/api/Auth/logout`, {}, { withCredentials: true })
      .pipe(take(1))
      .subscribe({
        next: () => {
          this.sessionService.clearSession();
          this.router.navigate(['/auth/login']);
        },
        error: () => {
          this.sessionService.clearSession();
          this.router.navigate(['/auth/login']);
        }
      });
  }
}
