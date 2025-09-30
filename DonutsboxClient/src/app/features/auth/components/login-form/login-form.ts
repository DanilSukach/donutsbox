import { Component, EventEmitter, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { LoginRequestDto } from '@app/api/auth';

@Component({
  selector: 'app-login-form',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './login-form.html',
  styleUrl: './login-form.css'
})
export class LoginForm {
  @Output() login = new EventEmitter<LoginRequestDto>();

  model: LoginRequestDto = {
    emailAuth: '',
    password: ''
  };

  submit(): void {
    this.login.emit(this.model);
  }
}


