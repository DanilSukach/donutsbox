import { isPlatformServer } from '@angular/common';
import { inject, Injectable, PLATFORM_ID, signal } from '@angular/core';
import { Observable, of, shareReplay, catchError, tap, finalize, retry, timer } from 'rxjs';
import { SessionService as ApiSessionService, SessionInfoDto } from '@app/api/donutsbox';
import { HttpErrorResponse } from '@angular/common/http';

@Injectable({ providedIn: 'root' })
export class SessionService {
  private apiSessionService = inject(ApiSessionService);
  private platformId = inject(PLATFORM_ID);
  private isServer = isPlatformServer(this.platformId);
  private current = signal<SessionInfoDto | null>(null);
  private loaded = signal(false);
  private pending$: Observable<SessionInfoDto | null> | null = null;

  session(): SessionInfoDto | null {
    return this.current();
  }

  userId(): string | null {
    return this.current()?.userId ?? null;
  }

  isCreator(): boolean {
    return this.current()?.isCreator ?? false;
  }

  hasCreatorPage(): boolean {
    return this.current()?.hasCreatorPage ?? false;
  }

  ensureSession(force = false): Observable<SessionInfoDto | null> {
    if (this.isServer) {
      return of(null);
    }

    if (!force && this.loaded()) {
      return of(this.current());
    }

    if (!this.pending$ || force) {
      this.pending$ = this.apiSessionService.apiSessionMeGet().pipe(
        // Интерцептор authRefreshInterceptor должен перехватить 401 и попытаться обновить токен
        // Если интерцептор успешно обновит токен, запрос будет повторен автоматически
        // Если интерцептор не сможет обновить токен, он вызовет handleRefreshFailure и пробросит ошибку
        // catchError здесь обрабатывает только финальную ошибку после всех попыток интерцептора
        catchError((error: HttpErrorResponse) => {
          console.log('🔐 SessionService: Error in ensureSession', error.status, error.url);
          // Если это 401, интерцептор уже попытался обновить токен
          // Если обновление не удалось, интерцептор пробросил ошибку дальше
          // В этом случае возвращаем null, что означает отсутствие сессии
          if (error.status === 401) {
            console.log('🔐 SessionService: 401 error, clearing session');
            this.current.set(null);
            this.loaded.set(true);
            return of(null);
          }
          throw error;
        }),
        tap((session) => {
          if (session) {
            console.log('✅ SessionService: Session loaded successfully', session.userId);
          }
          this.current.set(session);
          this.loaded.set(true);
        }),
        finalize(() => {
          this.pending$ = null;
        }),
        shareReplay(1)
      );
    }

    return this.pending$;
  }

  refreshSession(): Observable<SessionInfoDto | null> {
    this.loaded.set(false);
    return this.ensureSession(true);
  }

  clearSession(): void {
    this.current.set(null);
    this.loaded.set(false);
  }
}

