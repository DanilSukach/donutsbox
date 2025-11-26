import { HttpClient } from '@angular/common/http';
import { isPlatformServer } from '@angular/common';
import { inject, Injectable, PLATFORM_ID, signal } from '@angular/core';
import { Observable, of, shareReplay, catchError, tap, finalize } from 'rxjs';

export interface SessionInfo {
  userId: string;
  displayName?: string | null;
  email?: string | null;
  role: string;
  isCreator: boolean;
  hasCreatorPage: boolean;
  creatorPageId?: string | null;
}

@Injectable({ providedIn: 'root' })
export class SessionService {
  private http = inject(HttpClient);
  private platformId = inject(PLATFORM_ID);
  private isServer = isPlatformServer(this.platformId);
  private current = signal<SessionInfo | null>(null);
  private loaded = signal(false);
  private pending$: Observable<SessionInfo | null> | null = null;

  session(): SessionInfo | null {
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

  ensureSession(force = false): Observable<SessionInfo | null> {
    if (this.isServer) {
      return of(null);
    }

    if (!force && this.loaded()) {
      return of(this.current());
    }

    if (!this.pending$ || force) {
      this.pending$ = this.http.get<SessionInfo>('/api/session/me').pipe(
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

  refreshSession(): Observable<SessionInfo | null> {
    this.loaded.set(false);
    return this.ensureSession(true);
  }

  clearSession(): void {
    this.current.set(null);
    this.loaded.set(false);
  }
}

