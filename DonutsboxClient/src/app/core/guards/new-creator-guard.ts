import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { TokenService } from '../services/token.service';
import { JwtDecodeService } from '../services/jwt-decode.service';

export const newCreatorGuard: CanActivateFn = (route, state) => {
  const tokenService = inject(TokenService);
  const jwtService = inject(JwtDecodeService);
  const router = inject(Router);

  const token = tokenService.getAccessToken();
  
  if (!token) {
    router.navigate(['/auth/login']);
    return false;
  }

  if (!jwtService.isCreator(token)) {
    router.navigate(['/']);
    return false;
  }

  if (tokenService.isNewCreator()) {
    return true;
  }

  const userId = jwtService.getGuid(token);
  if (userId) {
    router.navigate(['/profile', userId]);
  } else {
    router.navigate(['/']);
  }
  return false;
};
