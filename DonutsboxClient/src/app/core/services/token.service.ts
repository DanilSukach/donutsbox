import { Injectable } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class TokenService {
  private readonly accessKey = 'db_access_token';
  private readonly refreshKey = 'db_refresh_token';
  private readonly isNewCreatorKey = 'db_is_new_creator';

  private get isBrowser(): boolean {
    return typeof window !== 'undefined' && typeof localStorage !== 'undefined';
  }

  private safeGetItem(key: string): string | null {
    if (!this.isBrowser) return null;
    try { return localStorage.getItem(key); } catch { return null; }
  }

  private safeSetItem(key: string, value: string): void {
    if (!this.isBrowser) return;
    try { localStorage.setItem(key, value); } catch { /* ignore */ }
  }

  private safeRemoveItem(key: string): void {
    if (!this.isBrowser) return;
    try { localStorage.removeItem(key); } catch { /* ignore */ }
  }

  setTokens(accessToken: string | null | undefined, refreshToken: string | null | undefined): void {
    if (accessToken) {
      this.safeSetItem(this.accessKey, accessToken);
      this.checkIsNewCreator(accessToken);
    } else {
      this.safeRemoveItem(this.accessKey);
      this.safeRemoveItem(this.isNewCreatorKey);
    }
    if (refreshToken) {
      this.safeSetItem(this.refreshKey, refreshToken);
    } else {
      this.safeRemoveItem(this.refreshKey);
    }
  }

  getAccessToken(): string | null {
    return this.safeGetItem(this.accessKey);
  }

  clear(): void {
    this.safeRemoveItem(this.accessKey);
    this.safeRemoveItem(this.refreshKey);
    this.safeRemoveItem(this.isNewCreatorKey);
  }

  isNewCreator(): boolean {
    return this.safeGetItem(this.isNewCreatorKey) === 'true';
  }

  clearNewCreatorStatus(): void {
    this.safeRemoveItem(this.isNewCreatorKey);
  }

  private checkIsNewCreator(token: string): void {
    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      if (payload.is_new_creator) {
        this.safeSetItem(this.isNewCreatorKey, 'true');
      } else {
        this.safeRemoveItem(this.isNewCreatorKey);
      }
    } catch (e) {
      console.error('Failed to parse token', e);
      this.safeRemoveItem(this.isNewCreatorKey);
    }
  }
}


