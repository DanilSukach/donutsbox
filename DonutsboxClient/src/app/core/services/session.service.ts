import { isPlatformServer } from '@angular/common';
import { inject, Injectable, PLATFORM_ID, signal } from '@angular/core';
import { Observable, of, shareReplay, catchError, tap, finalize } from 'rxjs';
import { SessionService as ApiSessionService, SessionInfoDto } from '@app/api/donutsbox';

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
        catchError((error) => {
          if (error.status === 401) {
            return of(null);
          }
          throw error;
        }),
        tap((session) => {
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

