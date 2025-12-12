import { ChangeDetectorRef, Component, inject } from '@angular/core';
import { AuthFacade } from '../../services/auth-facade';
import { RegisterRequestDto } from '@app/api/auth';
import { CommonModule } from '@angular/common';
import { RegisterForm } from '../../components/register-form/register-form';
import { RouterModule } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { LucideAngularModule } from 'lucide-angular';

@Component({
  selector: 'app-register-page',
  imports: [CommonModule, RegisterForm, RouterModule, LucideAngularModule],
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
        
        // Handle validation errors with field information
        if (err.error?.field && err.error?.message) {
          const field = err.error.field;
          const message = err.error.message;
          
          // Set the error on the corresponding field in the form
          // This will be handled by the RegisterForm component
          this.serverError = this.getErrorMessage(message);
        } else if (err.status === 409) {
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
      'Email is required': 'Email обязателен для заполнения',
      'Invalid email format': 'Некорректный формат email',
      'Email exists': 'Пользователь с таким email уже существует',
      'Password is required': 'Пароль обязателен для заполнения',
      'Password must be at least 8 characters long': 'Пароль должен содержать минимум 8 символов',
      'Password must not exceed 128 characters': 'Пароль не должен превышать 128 символов',
      'Password cannot consist only of digits': 'Пароль не может состоять только из цифр',
      'Password must contain at least one digit or special character': 'Пароль должен содержать хотя бы одну цифру или специальный символ',
      'Password must contain at least one uppercase letter': 'Пароль должен содержать хотя бы одну заглавную букву',
      'Password must contain at least one lowercase letter': 'Пароль должен содержать хотя бы одну строчную букву',
      'Password must contain at least one digit': 'Пароль должен содержать хотя бы одну цифру',
      'Password must contain at least one special character (!@#$%^&*()_+-=[]{}|;:,.<>?)': 'Пароль должен содержать хотя бы один специальный символ (!@#$%^&*()_+-=[]{}|;:,.<>?)',
      "Password doesn't match": 'Пароли не совпадают',
      'Administrator role cannot be created through registration':
        'Невозможно создать администратора через регистрацию',
    };

    return errorMessages[message] || message;
  }
}
