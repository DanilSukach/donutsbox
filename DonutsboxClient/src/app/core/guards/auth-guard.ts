import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { TokenService } from '../services/token.service';
import { JwtDecodeService } from '../services/jwt-decode.service';

export const authGuard: CanActivateFn = (route, state) => {
  const tokenService = inject(TokenService);
  const jwtService = inject(JwtDecodeService);
  const router = inject(Router);

  const token = tokenService.getAccessToken();
  
  if (!token) {
    router.navigate(['/auth/login']);
    return false;
  }

  const userGuid = jwtService.getGuid(token);
  if (!userGuid) {
    tokenService.clear();
    router.navigate(['/auth/login']);
    return false;
  }

  const isNewCreator = tokenService.isNewCreator();
  
  if (isNewCreator) {
    router.navigate(['/profile/setup']);
    return false;
  }

  return true;
};
