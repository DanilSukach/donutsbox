import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { TokenService } from '../services/token.service';
import { JwtDecodeService } from '../services/jwt-decode.service';

export const creatorGuard: CanActivateFn = (route, state) => {
  const tokenService = inject(TokenService);
  const jwtService = inject(JwtDecodeService);
  const router = inject(Router);

  const token = tokenService.getAccessToken();
  
  // Если токена нет, перенаправляем на логин
  if (!token) {
    router.navigate(['/auth/login']);
    return false;
  }

  // Проверяем, что пользователь является создателем
  if (!jwtService.isCreator(token)) {
    router.navigate(['/']);
    return false;
  }

  // Создатель может получить доступ
  return true;
};
