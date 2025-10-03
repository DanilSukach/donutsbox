import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LoginForm } from '../../components/login-form/login-form';
import { AuthFacade } from '../../services/auth-facade';
import { LoginRequestDto } from '@app/api/auth';
import { RouterModule } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { Router } from '@angular/router';

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

  protected errorMessage: string | null = null;

  onLogin(data: LoginRequestDto): void {
    this.errorMessage = null;

    this.authFacade.login(data).subscribe({
      next: ({ guid, isNewCreator }) => {
        console.log('Успешный вход', { guid, isNewCreator });

        if (isNewCreator) {
          this.router.navigate(['/profile/setup']);
        } else if (guid) {
          this.router.navigate(['/feed']);
        } else {
          this.router.navigate(['/']);
        }
      },
      error: (err: HttpErrorResponse) => {
        console.error('Ошибка входа', err);
        if (err.status === 401) {
          this.errorMessage = 'Неверный email или пароль.';
        } else {
          this.errorMessage = 'Произошла ошибка. Попробуйте снова.';
        }
      },
    });
  }
}
