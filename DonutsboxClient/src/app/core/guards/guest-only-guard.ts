import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { TokenService } from '../services/token.service';
import { JwtDecodeService } from '../services/jwt-decode.service';

export const guestOnlyGuard: CanActivateFn = (route, state) => {
  const tokenService = inject(TokenService);
  const jwt = inject(JwtDecodeService);
  const router = inject(Router);

  const token = tokenService.getAccessToken();
  if (!token) {
    return true;
  }

  const userId = jwt.getGuid(token);
  if (userId) {
    router.navigate(['/profile', userId]);
  } else {
    router.navigate(['/auth/login']);
  }
  return false;
};


