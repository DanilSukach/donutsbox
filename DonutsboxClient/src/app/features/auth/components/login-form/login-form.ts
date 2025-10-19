import { Component, EventEmitter, Input, Output, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { LoginRequestDto } from '@app/api/auth';

@Component({
  selector: 'app-login-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './login-form.html',
  styleUrl: './login-form.css'
})
export class LoginForm implements OnChanges {
  @Output() login = new EventEmitter<LoginRequestDto>();
  @Input() serverError: string | null = null;
  @Input() disabled = false;

  private fb = new FormBuilder();
  hasServerError = false;

  loginForm = this.fb.group({
    emailAuth: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(6)]]
  });

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['disabled']) {
      if (this.disabled) {
        this.loginForm.disable();
      } else {
        this.loginForm.enable();
      }
    }

    if (changes['serverError']) {
      if (this.serverError) {
        this.hasServerError = true;
        const emailControl = this.loginForm.get('emailAuth');
        const passwordControl = this.loginForm.get('password');
        emailControl?.markAsTouched();
        passwordControl?.markAsTouched();
      } else {
        this.hasServerError = false;
      }
    }
  }

  submit(): void {
    Object.keys(this.loginForm.controls).forEach(key => {
      this.loginForm.get(key)?.markAsTouched();
    });

    if (this.loginForm.valid && !this.disabled) {
      this.hasServerError = false;
      this.login.emit(this.loginForm.getRawValue() as LoginRequestDto);
    }
  }

  getFieldError(fieldName: string): string | null {
    const field = this.loginForm.get(fieldName);
    
    if (fieldName === 'emailAuth' && this.hasServerError && this.serverError) {
      return this.serverError;
    }
    
    if (field?.invalid && field?.touched && !this.hasServerError) {
      if (field.errors?.['required']) {
        return 'Это поле обязательно';
      }
      if (field.errors?.['email']) {
        return 'Введите корректный email';
      }
      if (field.errors?.['minlength']) {
        return 'Минимум 6 символов';
      }
    }
    return null;
  }

  hasFieldError(fieldName: string): boolean {
    const field = this.loginForm.get(fieldName);
    return this.hasServerError || !!(field?.invalid && field?.touched);
  }

  isFieldValid(fieldName: string): boolean {
    const field = this.loginForm.get(fieldName);
    return !this.hasServerError && !!(field?.valid && field?.touched);
  }

  canSubmit(): boolean {
    return this.loginForm.valid && !this.disabled;
  }
}