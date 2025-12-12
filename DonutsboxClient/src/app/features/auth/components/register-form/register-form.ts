import { Component, EventEmitter, inject, Input, Output, SimpleChanges, signal, HostListener } from '@angular/core';
import {
  FormBuilder,
  Validators,
  ReactiveFormsModule,
  AbstractControl,
} from '@angular/forms';
import { RegisterRequestDto } from '@app/api/auth';
import { LucideAngularModule } from 'lucide-angular';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-register-form',
  imports: [ReactiveFormsModule, LucideAngularModule, CommonModule],
  templateUrl: './register-form.html',
  styleUrl: './register-form.css'
})
export class RegisterForm {
  @Output() register = new EventEmitter<RegisterRequestDto>();
  @Input() disabled = false;
  @Input() serverError: string | null = null; // Добавляем Input для серверной ошибки

  readonly isRoleDropdownOpen = signal(false);

  private fb = inject(FormBuilder);

  registerForm = this.fb.group({
    authEmail: ['', [Validators.required, this.emailValidator]],
    password: ['', [
      Validators.required, 
      Validators.minLength(8),
      Validators.maxLength(128),
      this.passwordValidator // Добавляем кастомный валидатор
    ]],
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
    
    // Когда приходит серверная ошибка, показываем её под соответствующим полем
    if (changes['serverError'] && this.serverError) {
      // Определяем поле по содержимому ошибки
      let targetField: string | null = null;
      
      if (this.serverError.includes('email') || this.serverError.includes('Email')) {
        targetField = 'authEmail';
      } else if (this.serverError.includes('парол') || this.serverError.includes('Парол') || 
                 this.serverError.includes('password') || this.serverError.includes('Password')) {
        // Проверяем, относится ли ошибка к повторному паролю
        if (this.serverError.includes('совпадают') || this.serverError.includes("doesn't match")) {
          targetField = 'repeatPassword';
        } else {
          targetField = 'password';
        }
      }
      
      if (targetField) {
        const control = this.registerForm.get(targetField);
        if (control) {
          control.setErrors({ serverError: this.serverError });
          control.markAsTouched();
        }
      }
    }
  }

  private emailValidator(control: AbstractControl): { [key: string]: any } | null {
    if (!control.value) {
      return null; // required validator обработает это
    }
    
    const email = control.value.trim().toLowerCase();
    const emailRegex = /^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$/;
    
    if (!emailRegex.test(email)) {
      return { email: true };
    }
    
    // Дополнительные проверки
    if (email.length > 254) {
      return { email: true };
    }
    
    const parts = email.split('@');
    if (parts.length !== 2) {
      return { email: true };
    }
    
    const [localPart, domain] = parts;
    
    // Проверка локальной части
    if (localPart.length === 0 || localPart.length > 64) {
      return { email: true };
    }
    
    // Проверка домена
    if (domain.length === 0 || domain.length > 253) {
      return { email: true };
    }
    
    // Проверка, что домен содержит точку
    if (!domain.includes('.')) {
      return { email: true };
    }
    
    // Проверка, что домен не начинается и не заканчивается точкой или дефисом
    if (domain.startsWith('.') || domain.endsWith('.') || 
        domain.startsWith('-') || domain.endsWith('-')) {
      return { email: true };
    }
    
    return null;
  }

  private passwordValidator(control: AbstractControl): { [key: string]: any } | null {
    if (!control.value) {
      return null; // required validator обработает это
    }

    const password = control.value;
    const errors: { [key: string]: boolean } = {};

    // Проверка: не только цифры
    if (/^\d+$/.test(password)) {
      errors['onlyDigits'] = true;
    }

    // Проверка: не только буквы
    if (/^[a-zA-Z]+$/.test(password)) {
      errors['onlyLetters'] = true;
    }

    // Проверка: есть заглавные буквы
    if (!/[A-Z]/.test(password)) {
      errors['noUppercase'] = true;
    }

    // Проверка: есть строчные буквы
    if (!/[a-z]/.test(password)) {
      errors['noLowercase'] = true;
    }

    // Проверка: есть цифры
    if (!/\d/.test(password)) {
      errors['noDigit'] = true;
    }

    // Проверка: есть специальные символы
    if (!/[!@#$%^&*()_+\-=\[\]{}|;:,.<>?]/.test(password)) {
      errors['noSpecialChar'] = true;
    }

    return Object.keys(errors).length > 0 ? errors : null;
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
      // Очищаем серверные ошибки при новой отправке
      ['authEmail', 'password', 'repeatPassword'].forEach(fieldName => {
        const control = this.registerForm.get(fieldName);
        if (control?.hasError('serverError')) {
          const errors = { ...control.errors };
          delete errors['serverError'];
          control.setErrors(Object.keys(errors).length > 0 ? errors : null);
        }
      });
      
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
        return 'Минимум 8 символов';
      }
      if (field.errors?.['maxlength']) {
        return 'Максимум 128 символов';
      }
      if (field.errors?.['onlyDigits']) {
        return 'Пароль не может состоять только из цифр';
      }
      if (field.errors?.['onlyLetters']) {
        return 'Пароль должен содержать хотя бы одну цифру или специальный символ';
      }
      if (field.errors?.['noUppercase']) {
        return 'Пароль должен содержать хотя бы одну заглавную букву';
      }
      if (field.errors?.['noLowercase']) {
        return 'Пароль должен содержать хотя бы одну строчную букву';
      }
      if (field.errors?.['noDigit']) {
        return 'Пароль должен содержать хотя бы одну цифру';
      }
      if (field.errors?.['noSpecialChar']) {
        return 'Пароль должен содержать хотя бы один специальный символ (!@#$%^&*()_+-=[]{}|;:,.<>?)';
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

  toggleRoleDropdown(): void {
    if (!this.disabled) {
      this.isRoleDropdownOpen.set(!this.isRoleDropdownOpen());
    }
  }

  selectRole(role: string): void {
    this.registerForm.patchValue({ role });
    this.isRoleDropdownOpen.set(false);
  }

  getRoleLabel(role: string | null | undefined): string {
    switch (role) {
      case 'User':
        return 'Пользователь';
      case 'Creator':
        return 'Автор';
      default:
        return 'Пользователь';
    }
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    const target = event.target as HTMLElement;
    if (!target.closest('.role-dropdown-container')) {
      this.isRoleDropdownOpen.set(false);
    }
  }
}
