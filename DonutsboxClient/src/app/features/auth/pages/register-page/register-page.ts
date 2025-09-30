import { Component, inject } from '@angular/core';
import { AuthFacade } from '../../services/auth-facade';
import { RegisterRequestDto } from '@app/api/auth';
import { CommonModule } from '@angular/common';
import { RegisterForm } from '../../components/register-form/register-form';

@Component({
  selector: 'app-register-page',
  imports: [CommonModule, RegisterForm],
  templateUrl: './register-page.html',
  styleUrl: './register-page.css',
})
export class RegisterPage {
  private authFacade = inject(AuthFacade);

  onRegister(registerData: RegisterRequestDto): void {
    this.authFacade.register(registerData).subscribe({
      next: () => {
        console.log('Регистрация прошла успешно!');
      },
      error: (err) => {
        console.error('Ошибка регистрации', err);
      },
    });
  }
}
