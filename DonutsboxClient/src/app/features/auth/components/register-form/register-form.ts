import { Component, EventEmitter, inject, Output } from '@angular/core';
import {
  FormBuilder,
  Validators,
  ReactiveFormsModule,
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

  private fb = inject(FormBuilder);

  registerForm = this.fb.group({
    authEmail: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(6)]],
    repeatPassword: ['', [Validators.required]],
    role: ['User', Validators.required],
  }, { 
  });

  onSubmit(): void {
    if (this.registerForm.valid) {
      this.register.emit(this.registerForm.getRawValue() as RegisterRequestDto);
    }
  }
}
