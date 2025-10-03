import { Injectable } from '@angular/core';
import { jwtDecode } from 'jwt-decode';

interface DecodedToken {
  'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier': string;
  'http://schemas.microsoft.com/ws/2008/06/identity/claims/role': string;
  'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress': string;
  is_new_creator?: 'true' | 'false';
  exp: number;
  iss: string;
  aud: string;
}

@Injectable({ providedIn: 'root' })
export class JwtDecodeService {
  private decodeToken(token: string | null): DecodedToken | null {
    if (!token) {
      return null;
    }
    try {
      return jwtDecode<DecodedToken>(token);
    } catch (error) {
      console.error('Failed to decode token', error);
      return null;
    }
  }

  getGuid(token: string | null): string | null {
    const decoded = this.decodeToken(token);
    return (
      decoded?.['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'] ?? null
    );
  }

  isNewCreator(token: string | null): boolean {
    const decoded: any = this.decodeToken(token);
    return decoded?.['is_new_creator'] === 'true';
  }

  getRole(token: string | null): string | null {
    const decoded = this.decodeToken(token);
    return decoded?.['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] ?? null;
  }

  isCreator(token: string | null): boolean {
    const role = this.getRole(token);
    return role === 'Creator';
  }
}
