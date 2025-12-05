import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';

export const notFoundInterceptor: HttpInterceptorFn = (req, next) => {
  const router = inject(Router);

  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      // Обрабатываем только 404 ошибки от API запросов
      if (error.status === 404) {
        const url = req.url;
        
        // Проверяем, что это запрос к API (не статический ресурс)
        const isApiRequest = url.includes('/api/');
        
        // Паттерны URL, для которых 404 должен перенаправлять на страницу 404
        const redirectPatterns = [
          '/api/User/', // Пользователи - если не найден, значит страница не существует
        ];
        
        // Исключаем некоторые эндпоинты, где 404 может быть нормальным поведением
        // или обрабатывается в компонентах
        const excludedPaths = [
          '/api/Files/', // Файлы могут не существовать
          '/api/session/', // Сессия может не быть
          '/api/Auth/', // Auth эндпоинты обрабатываются отдельно
          '/api/Authors/', // Авторы обрабатываются в profile-page.ts
        ];
        
        const shouldRedirect = isApiRequest && 
          redirectPatterns.some(pattern => url.includes(pattern)) &&
          !excludedPaths.some(path => url.includes(path));
        
        if (shouldRedirect) {
          // Перенаправляем на страницу 404
          router.navigate(['/404']);
          return throwError(() => error);
        }
      }
      
      return throwError(() => error);
    })
  );
};
