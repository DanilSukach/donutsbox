import { Component, inject, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { UserProfileFacade } from '@app/core/services/user-profile-facade';
import { LucideAngularModule } from 'lucide-angular';

@Component({
  selector: 'app-change-email-modal',
  standalone: true,
  imports: [FormsModule, LucideAngularModule],
  templateUrl: './change-email-modal.html',
  styleUrl: './change-email-modal.css'
})
export class ChangeEmailModal {
  private userProfileFacade = inject(UserProfileFacade);

  readonly closed = output<void>();
  readonly emailChanged = output<void>();

  newEmailValue = '';
  
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
    if (!this.newEmailValue) {
      this.error.set('Email обязателен');
      return;
    }

    // Простая валидация email
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    if (!emailRegex.test(this.newEmailValue)) {
      this.error.set('Введите корректный email');
      return;
    }

    this.isSubmitting.set(true);

    const dto = {
      email: this.newEmailValue
    };

    this.userProfileFacade.changeEmail(dto).subscribe(result => {
      this.isSubmitting.set(false);
      
      if (result.success) {
        this.emailChanged.emit();
        this.closed.emit();
      } else {
        this.error.set(result.message || 'Неизвестная ошибка');
      }
    });
  }
}

