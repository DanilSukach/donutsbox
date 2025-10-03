import { inject, Injectable } from '@angular/core';
import { Router } from '@angular/router';
import { AuthResponseDto, AuthService, LoginRequestDto, RegisterRequestDto } from '@app/api/auth';
import { map, tap } from 'rxjs';
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
    return this.authApiService.apiAuthRegisterPost(registerData).pipe(
      tap(() => {
        this.router.navigate(['/auth/login']);
      })
    );
  }

login(loginData: LoginRequestDto) {
  return this.authApiService.apiAuthLoginPost(loginData).pipe(
    map((resp: AuthResponseDto) => {
      this.tokenService.setTokens(resp.accessToken ?? undefined, resp.refreshToken ?? undefined);
      const guid = this.jwtDecode.getGuid(resp.accessToken ?? null);
      const isNewCreator = this.jwtDecode.isNewCreator(resp.accessToken ?? null);

      return { guid, isNewCreator };
    })
  );
}

  logout(): void {
    this.tokenService.clear();
    this.router.navigate(["/auth/login"]);
  }
}
