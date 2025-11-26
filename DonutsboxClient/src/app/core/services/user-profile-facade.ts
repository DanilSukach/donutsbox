import { inject, Injectable } from '@angular/core';
import { NewPasswordDto, NewEmailDto, UserProfileService } from '@app/api/auth';
import { catchError, map, Observable, of } from 'rxjs';

export interface ChangePasswordResult {
  success: boolean;
  message?: string;
}

export interface ChangeEmailResult {
  success: boolean;
  message?: string;
}

@Injectable({
  providedIn: 'root'
})
export class UserProfileFacade {
  private userProfileService = inject(UserProfileService);

  changePassword(dto: NewPasswordDto): Observable<ChangePasswordResult> {
    return this.userProfileService.apiUserProfileChangePasswordPut(dto).pipe(
      map((response: any) => ({
        success: true,
        message: response?.message || 'Пароль успешно изменён'
      })),
      catchError((error) => {
        let errorMessage = 'Ошибка при смене пароля';
        
        if (error.status === 401) {
          errorMessage = error.error?.message || 'Неверный старый пароль';
        } else if (error.status === 400) {
          errorMessage = error.error?.message || 'Ошибка валидации';
        } else if (error.status === 0) {
          errorMessage = 'Нет соединения с сервером';
        } else if (error.error?.message) {
          errorMessage = error.error.message;
        }
        
        return of({
          success: false,
          message: errorMessage
        });
      })
    );
  }

  changeEmail(dto: NewEmailDto): Observable<ChangeEmailResult> {
    return this.userProfileService.apiUserProfileChangeEmailPut(dto).pipe(
      map((response: any) => ({
        success: true,
        message: response?.message || 'Email успешно изменён'
      })),
      catchError((error) => {
        let errorMessage = 'Ошибка при смене email';
        
        if (error.status === 400) {
          errorMessage = error.error?.message || error.error || 'Такой email уже используется';
        } else if (error.status === 0) {
          errorMessage = 'Нет соединения с сервером';
        } else if (error.error?.message) {
          errorMessage = error.error.message;
        } else if (typeof error.error === 'string') {
          errorMessage = error.error;
        }
        
        return of({
          success: false,
          message: errorMessage
        });
      })
    );
  }
}

