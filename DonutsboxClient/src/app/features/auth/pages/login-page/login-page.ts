import { Component, inject, ChangeDetectorRef } from '@angular/core';
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
  private cdr = inject(ChangeDetectorRef);

  protected serverError: string | null = null;
  protected isLoading = false;

  onLogin(data: LoginRequestDto): void {
    this.serverError = null;
    this.isLoading = true;
    this.cdr.detectChanges();

    this.authFacade.login(data).subscribe({
      next: ({ guid, isNewCreator }) => {
        this.isLoading = false;
        this.cdr.detectChanges();

        if (isNewCreator) {
          this.router.navigate(['/profile/setup']);
        } else if (guid) {
          this.router.navigate(['/feed']);
        } else {
          this.router.navigate(['/']);
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