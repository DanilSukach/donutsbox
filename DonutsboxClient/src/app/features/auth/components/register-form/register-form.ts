import { Component, EventEmitter, inject, Input, Output, SimpleChanges } from '@angular/core';
import {
  FormBuilder,
  Validators,
  ReactiveFormsModule,
  AbstractControl,
} from '@angular/forms';
import { RegisterRequestDto } from '@app/api/auth';

@Component({
  selector: 'app-register-form',
  imports: [ReactiveFormsModule],
  templateUrl: './register-form.html',
  styleUrl: './register-form.css'
})
export class RegisterForm {
  @Output() register = new EventEmitter<RegisterRequestDto>();
  @Input() disabled = false;
  @Input() serverError: string | null = null; // Добавляем Input для серверной ошибки

  private fb = inject(FormBuilder);

  registerForm = this.fb.group({
    authEmail: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(6)]],
    repeatPassword: ['', [Validators.required]],
    role: ['User', Validators.required],
  }, { 
    validators: this.passwordMatchValidator
  });

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['disabled']) {
      if (this.disabled) {
        this.registerForm.disable();
      } else {
        this.registerForm.enable();
      }
    }
    
    // Когда приходит серверная ошибка, показываем её под полем email
    if (changes['serverError'] && this.serverError) {
      const emailControl = this.registerForm.get('authEmail');
      if (emailControl) {
        emailControl.setErrors({ serverError: this.serverError });
        emailControl.markAsTouched();
      }
    }
  }

  private passwordMatchValidator(form: AbstractControl) {
    const password = form.get('password');
    const repeatPassword = form.get('repeatPassword');
    
    if (password && repeatPassword && password.value !== repeatPassword.value) {
      repeatPassword.setErrors({ passwordMismatch: true });
      return { passwordMismatch: true };
    }
    
    return null;
  }

  onSubmit(): void {
    if (this.registerForm.valid && !this.disabled) {
      // Очищаем серверную ошибку при новой отправке
      const emailControl = this.registerForm.get('authEmail');
      if (emailControl?.hasError('serverError')) {
        const errors = { ...emailControl.errors };
        delete errors['serverError'];
        emailControl.setErrors(Object.keys(errors).length > 0 ? errors : null);
      }
      
      this.register.emit(this.registerForm.getRawValue() as RegisterRequestDto);
    }
  }

  getFieldError(fieldName: string): string | null {
    const field = this.registerForm.get(fieldName);
    if (field?.invalid && field?.touched) {
      if (field.errors?.['required']) {
        return 'Это поле обязательно';
      }
      if (field.errors?.['email']) {
        return 'Введите корректный email';
      }
      if (field.errors?.['minlength']) {
        return 'Минимум 6 символов';
      }
      if (field.errors?.['passwordMismatch']) {
        return 'Пароли не совпадают';
      }
      if (field.errors?.['serverError']) {
        return field.errors['serverError'];
      }
    }
    return null;
  }

  hasFieldError(fieldName: string): boolean {
    const field = this.registerForm.get(fieldName);
    return !!(field?.invalid && field?.touched);
  }

  isFieldValid(fieldName: string): boolean {
    const field = this.registerForm.get(fieldName);
    return !!(field?.valid && field?.touched);
  }
}
