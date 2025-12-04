import { Component, inject, output, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { SessionService } from '@app/core/services/session.service';
import { Router } from '@angular/router';
import { UserService, FirstLoginDto } from '@app/api/donutsbox';

@Component({
  selector: 'app-first-login-modal',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './first-login-modal.html',
  styleUrl: './first-login-modal.css'
})
export class FirstLoginModal {
  private userService = inject(UserService);
  private sessionService = inject(SessionService);
  private router = inject(Router);
  private fb = inject(FormBuilder);

  readonly completed = output<void>();
  readonly closed = output<void>();

  readonly isLoading = signal(false);
  readonly error = signal<string | null>(null);

  firstLoginForm = this.fb.group({
    name: ['', [Validators.required, Validators.minLength(3), Validators.maxLength(50)]],
    phoneNumber: ['', [Validators.maxLength(11)]]
  });

  onSubmit(): void {
    if (this.firstLoginForm.invalid || this.isLoading()) {
      return;
    }

    this.isLoading.set(true);
    this.error.set(null);

    const formValue = this.firstLoginForm.getRawValue();
    const dto: FirstLoginDto = {
      name: formValue.name || null,
      phoneNumber: formValue.phoneNumber || null
    };

    this.userService.apiUserCompleteFirstLoginPost(dto).subscribe({
      next: () => {
        // Обновляем сессию
        this.sessionService.refreshSession().subscribe(() => {
          this.completed.emit();
          this.isLoading.set(false);
          // Если мы не на странице профиля, перенаправляем туда
          if (!this.router.url.startsWith('/profile/')) {
            const userId = this.sessionService.userId();
            if (userId) {
              this.router.navigate(['/profile', userId]);
            } else {
              this.router.navigate(['/feed']);
            }
          }
        });
      },
      error: (err: HttpErrorResponse) => {
        this.isLoading.set(false);
        if (err.error?.message) {
          this.error.set(err.error.message);
        } else {
          this.error.set('Произошла ошибка при сохранении данных');
        }
      }
    });
  }


  getFieldError(fieldName: string): string | null {
    const field = this.firstLoginForm.get(fieldName);
    if (field?.invalid && field?.touched) {
      if (field.errors?.['required']) {
        return 'Это поле обязательно';
      }
      if (field.errors?.['minlength']) {
        return 'Минимум 3 символа';
      }
      if (field.errors?.['maxlength']) {
        return fieldName === 'name' ? 'Максимум 50 символов' : 'Максимум 11 символов';
      }
    }
    return null;
  }

  hasFieldError(fieldName: string): boolean {
    const field = this.firstLoginForm.get(fieldName);
    return !!(field?.invalid && field?.touched);
  }

  close(): void {
    if (this.isLoading()) {
      return; // Не позволяем закрыть во время загрузки
    }
    
    // Обновляем LastAuth, чтобы пользователь мог войти в систему
    // При следующем входе модальное окно снова появится, пока данные не заполнены
    this.userService.apiUserSkipFirstLoginPost().subscribe({
      next: () => {
        // Обновляем сессию
        this.sessionService.refreshSession().subscribe(() => {
          this.closed.emit();
          // Если мы не на странице профиля, перенаправляем туда
          if (!this.router.url.startsWith('/profile/')) {
            const userId = this.sessionService.userId();
            if (userId) {
              this.router.navigate(['/profile', userId]);
            } else {
              this.router.navigate(['/feed']);
            }
          }
        });
      },
      error: () => {
        // В случае ошибки все равно закрываем
        this.closed.emit();
        // Если мы не на странице профиля, перенаправляем туда
        if (!this.router.url.startsWith('/profile/')) {
          const userId = this.sessionService.userId();
          if (userId) {
            this.router.navigate(['/profile', userId]);
          } else {
            this.router.navigate(['/feed']);
          }
        }
      }
    });
  }
}

