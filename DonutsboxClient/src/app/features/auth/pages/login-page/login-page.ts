import { Component, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LoginForm } from '../../components/login-form/login-form';
import { AuthFacade } from '../../services/auth-facade';
import { LoginRequestDto } from '@app/api/auth';
import { RouterModule } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { Router } from '@angular/router';
import { SessionService } from '@app/core/services/session.service';

@Component({
  selector: 'app-login-page',
  standalone: true,
  imports: [CommonModule, LoginForm, RouterModule],
  templateUrl: './login-page.html',
  styleUrl: './login-page.css',
})
export class LoginPage {
  private authFacade = inject(AuthFacade);
  private readonly router = inject(Router);
  private cdr = inject(ChangeDetectorRef);
  private sessionService = inject(SessionService);

  protected serverError: string | null = null;
  protected isLoading = false;

  onLogin(data: LoginRequestDto): void {
    this.serverError = null;
    this.isLoading = true;
    this.cdr.detectChanges();

    this.authFacade.login(data).subscribe({
      next: ({ guid, isNewCreator, isFirstLogin }) => {
        this.isLoading = false;
        this.cdr.detectChanges();

        // Проверяем роль пользователя после логина
        const session = this.sessionService.session();
        const isAdmin = session?.role === 'Administrator' || session?.role === 'Admin';
        
        // Если админ - всегда редиректим на /management
        if (isAdmin) {
          this.router.navigate(['/management']);
          return;
        }

        // Модальное окно первого входа теперь показывается на странице профиля
        if (isNewCreator) {
          this.router.navigate(['/profile/setup']);
        } else if (guid) {
          // Перенаправляем на профиль, где будет показано модальное окно, если нужно
          this.router.navigate(['/profile', guid]);
        } else {
          this.router.navigate(['/feed']);
        }
      },
      error: (err: HttpErrorResponse) => {
        this.isLoading = false;
        
        if (err.status === 401) {
          this.serverError = 'Неверный email или пароль';
        } else {
          this.serverError = 'Произошла ошибка. Попробуйте снова';
        }
        
        this.cdr.detectChanges();
      },
    });
  }

}