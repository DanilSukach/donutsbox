import { inject, Injectable } from '@angular/core';
import { Router } from '@angular/router';
import { AuthResponseDto, AuthService, LoginRequestDto, RegisterRequestDto } from '@app/api/auth';
import { tap } from 'rxjs';
import { TokenService } from '@app/core/services/token.service';
import { JwtDecodeService } from '@app/core/services/jwt-decode.service';


@Injectable({
  providedIn: 'root'
})
export class AuthFacade {
  private readonly authApiService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly tokenService = inject(TokenService);
  private readonly jwtDecode = inject(JwtDecodeService);

  register(registerData: RegisterRequestDto) {
    console.log('Register payload:', registerData);
    return this.authApiService.apiAuthRegisterPost(registerData).pipe(
      tap(() => {
        this.router.navigate(['/auth/login']);
      })
    );
  }

  login(loginData: LoginRequestDto) {
    console.log('Login payload:', loginData);
    return this.authApiService.apiAuthLoginPost(loginData).pipe(
      tap((resp: AuthResponseDto) => {
        console.log('Auth response:', resp);
        this.tokenService.setTokens(resp.accessToken ?? undefined, resp.refreshToken ?? undefined);
        const guid = this.jwtDecode.getGuid(resp.accessToken ?? null);
        console.log('Decoded GUID:', guid);
        if (guid) {
          console.log('Navigating to profile with GUID:', guid);
          this.router.navigate(['/profile', guid]);
        } else {
          console.warn('GUID not found in token, navigating to root');
          this.router.navigate(['/']);
        }
      })
    );
  }
}
