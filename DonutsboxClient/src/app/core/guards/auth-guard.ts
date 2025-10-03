import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { TokenService } from '../services/token.service';
import { JwtDecodeService } from '../services/jwt-decode.service';

export const authGuard: CanActivateFn = (route, state) => {
  const tokenService = inject(TokenService);
  const jwtService = inject(JwtDecodeService);
  const router = inject(Router);

  console.log('authGuard: проверка доступа к', state.url);

  const token = tokenService.getAccessToken();
  
  if (!token) {
    console.log('authGuard: токен отсутствует, перенаправление на логин');
    router.navigate(['/auth/login']);
    return false;
  }

  const userGuid = jwtService.getGuid(token);
  if (!userGuid) {
    console.log('authGuard: некорректный токен, очистка и перенаправление на логин');
    tokenService.clear();
    router.navigate(['/auth/login']);
    return false;
  }

  const isNewCreator = tokenService.isNewCreator();
  console.log('authGuard: пользователь новый создатель?', isNewCreator);
  
  if (isNewCreator) {
    console.log('authGuard: перенаправление нового создателя на настройку профиля');
    router.navigate(['/profile/setup']);
    return false;
  }

  console.log('authGuard: доступ разрешен');
  return true;
};
