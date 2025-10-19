import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@microsoft/signalr';
import { TokenService } from './token.service';
import { from, Observable, of, switchMap, tap } from 'rxjs';
import { AuthResponseDto, AuthService, RefreshRequestDto } from '@app/api/auth';

@Injectable({
  providedIn: 'root'
})
export class AuthRefresh {
 private authService = inject(AuthService);
  private tokenService = inject(TokenService);

  refreshAccessToken(): Observable<AuthResponseDto> {
    const refreshToken = this.tokenService.getRefreshToken();
    
    if (!refreshToken) {
      throw new Error('No refresh token available');
    }

    const request: RefreshRequestDto = {
      refreshToken: refreshToken
    };

    return this.authService.apiAuthRefreshPost(request).pipe(
      tap((response: AuthResponseDto) => {
        console.log('✅ Token refreshed successfully');
        this.tokenService.setTokens(response.accessToken, response.refreshToken);
      })
    );
  }
}
