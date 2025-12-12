import { Component, inject, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { UserProfileFacade } from '@app/core/services/user-profile-facade';
import { LucideAngularModule } from 'lucide-angular';

@Component({
  selector: 'app-change-password-modal',
  standalone: true,
  imports: [FormsModule, LucideAngularModule],
  templateUrl: './change-password-modal.html',
  styleUrl: './change-password-modal.css'
})
export class ChangePasswordModal {
  private userProfileFacade = inject(UserProfileFacade);

  readonly closed = output<void>();
  readonly passwordChanged = output<void>();

  oldPasswordValue = '';
  newPasswordValue = '';
  repeatNewPasswordValue = '';
  
  readonly isSubmitting = signal(false);
  readonly error = signal<string | null>(null);

  onClose(event?: Event): void {
    if (event) {
      event.stopPropagation();
    }
    this.closed.emit();
  }

  onModalContentClick(event: Event): void {
    event.stopPropagation();
  }

  onButtonClick(): void {
    this.error.set(null);

    // Валидация
    if (!this.oldPasswordValue || !this.newPasswordValue || !this.repeatNewPasswordValue) {
      this.error.set('Все поля обязательны');
      return;
    }

    if (this.newPasswordValue.length < 6) {
      this.error.set('Новый пароль должен содержать минимум 6 символов');
      return;
    }

    if (this.newPasswordValue !== this.repeatNewPasswordValue) {
      this.error.set('Новые пароли не совпадают');
      return;
    }

    if (this.oldPasswordValue === this.newPasswordValue) {
      this.error.set('Новый пароль должен отличаться от старого');
      return;
    }

    this.isSubmitting.set(true);

    const dto = {
      oldPassword: this.oldPasswordValue,
      newPassword: this.newPasswordValue,
      repeatNewPassword: this.repeatNewPasswordValue
    };

    this.userProfileFacade.changePassword(dto).subscribe(result => {
      this.isSubmitting.set(false);
      
      if (result.success) {
        this.passwordChanged.emit();
        this.closed.emit();
      } else {
        this.error.set(result.message || 'Неизвестная ошибка');
      }
    });
  }
}

