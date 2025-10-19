import { ChangeDetectorRef, Component, inject } from '@angular/core';
import { AuthFacade } from '../../services/auth-facade';
import { RegisterRequestDto } from '@app/api/auth';
import { CommonModule } from '@angular/common';
import { RegisterForm } from '../../components/register-form/register-form';
import { RouterModule } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';

@Component({
  selector: 'app-register-page',
  imports: [CommonModule, RegisterForm, RouterModule],
  templateUrl: './register-page.html',
  styleUrl: './register-page.css',
})
export class RegisterPage {
  private authFacade = inject(AuthFacade);
  private cdr = inject(ChangeDetectorRef);

  protected serverError: string | null = null;
  protected isLoading = false;

  onRegister(registerData: RegisterRequestDto): void {
    this.serverError = null;
    this.isLoading = true;
    this.cdr.detectChanges();

    this.authFacade.register(registerData).subscribe({
      next: () => {
        console.log('✅ Регистрация прошла успешно!');
        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: (err: HttpErrorResponse) => {
        console.error('❌ Ошибка регистрации');
        this.isLoading = false;
        
        if (err.status === 409) {
          this.serverError = 'Пользователь с таким email уже существует';
        } else if (err.error?.message) {
          this.serverError = this.getErrorMessage(err.error.message);
        } else {
          this.serverError = 'Произошла ошибка при регистрации';
        }
        
        this.cdr.detectChanges();
      },
    });
  }

  private getErrorMessage(message: string): string {
    const errorMessages: Record<string, string> = {
      'Email exists': 'Пользователь с таким email уже существует',
      "Password doesn't match": 'Пароли не совпадают',
      'Administrator role cannot be created through registration':
        'Невозможно создать администратора через регистрацию',
    };

    return errorMessages[message] || message;
  }
}
