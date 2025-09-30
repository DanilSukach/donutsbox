import { Injectable } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class TokenService {
  private readonly accessKey = 'db_access_token';
  private readonly refreshKey = 'db_refresh_token';

  setTokens(accessToken: string | null | undefined, refreshToken: string | null | undefined): void {
    if (accessToken) {
      localStorage.setItem(this.accessKey, accessToken);
    } else {
      localStorage.removeItem(this.accessKey);
    }
    if (refreshToken) {
      localStorage.setItem(this.refreshKey, refreshToken);
    } else {
      localStorage.removeItem(this.refreshKey);
    }
  }

  getAccessToken(): string | null {
    return localStorage.getItem(this.accessKey);
  }

  clear(): void {
    localStorage.removeItem(this.accessKey);
    localStorage.removeItem(this.refreshKey);
  }
}


