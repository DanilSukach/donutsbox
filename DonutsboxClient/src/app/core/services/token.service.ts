import { Injectable } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class TokenService {
  private readonly accessKey = 'db_access_token';
  private readonly refreshKey = 'db_refresh_token';
  private readonly isNewCreatorKey = 'db_is_new_creator';

  private readonly storage: Storage | null = this.resolveStorage();

  private resolveStorage(): Storage | null {
    if (typeof window === 'undefined') {
      return null;
    }

    const storages: Storage[] = [];
    if (typeof window.sessionStorage !== 'undefined') storages.push(window.sessionStorage);
    if (typeof window.localStorage !== 'undefined') storages.push(window.localStorage);

    for (const storage of storages) {
      try {
        const testKey = '__db_storage_test__';
        storage.setItem(testKey, testKey);
        storage.removeItem(testKey);
        return storage;
      } catch {
        continue;
      }
    }

    return null;
  }

  private safeGetItem(key: string): string | null {
    if (!this.storage) return null;
    try {
      return this.storage.getItem(key);
    } catch {
      return null;
    }
  }

  private safeSetItem(key: string, value: string): void {
    if (!this.storage) return;
    try {
      this.storage.setItem(key, value);
    } catch {
      /* ignore */
    }
  }

  private safeRemoveItem(key: string): void {
    if (!this.storage) return;
    try {
      this.storage.removeItem(key);
    } catch {
      /* ignore */
    }
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

  getRefreshToken(): string | null {
    return this.safeGetItem(this.refreshKey);
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


