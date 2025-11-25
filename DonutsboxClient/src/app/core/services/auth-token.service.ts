import { Injectable, PLATFORM_ID, inject } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';

const REFRESH_TOKEN_KEY = 'donutsbox_refresh_token';

@Injectable({ providedIn: 'root' })
export class AuthTokenService {
  private platformId = inject(PLATFORM_ID);

  private isBrowser(): boolean {
    return isPlatformBrowser(this.platformId);
  }

  getRefreshToken(): string | null {
    if (!this.isBrowser()) {
      return null;
    }
    return localStorage.getItem(REFRESH_TOKEN_KEY);
  }

  setRefreshToken(token: string | null | undefined): void {
    if (!this.isBrowser()) {
      return;
    }

    if (token) {
      localStorage.setItem(REFRESH_TOKEN_KEY, token);
    } else {
      localStorage.removeItem(REFRESH_TOKEN_KEY);
    }
  }

  clear(): void {
    this.setRefreshToken(null);
  }
}


