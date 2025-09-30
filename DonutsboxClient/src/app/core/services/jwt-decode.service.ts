import { Injectable } from '@angular/core';

interface JwtPayload {
  sub?: string;
  guid?: string;
  [key: string]: unknown;
}

@Injectable({ providedIn: 'root' })
export class JwtDecodeService {
  decode<T = JwtPayload>(token: string | null | undefined): T | null {
    if (!token) return null;
    const parts = token.split('.');
    if (parts.length !== 3) return null;
    try {
      const payloadJson = atob(parts[1].replace(/-/g, '+').replace(/_/g, '/'));
      return JSON.parse(payloadJson) as T;
    } catch {
      return null;
    }
  }

  getGuid(token: string | null | undefined): string | null {
    const payload = this.decode(token);
    if (!payload) return null;
    const p = payload as Record<string, unknown>;
    const candidates = [
      'guid',
      'sub',
      'nameid',
      'nameidentifier',
      'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier',
    ];
    for (const key of candidates) {
      const val = p[key];
      if (typeof val === 'string' && val.length > 0) {
        return val;
      }
    }
    return null;
  }
}

 