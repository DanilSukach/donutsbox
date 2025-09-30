import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LoginForm } from '../../components/login-form/login-form';
import { AuthFacade } from '../../services/auth-facade';
import { LoginRequestDto } from '@app/api/auth';

@Component({
  selector: 'app-login-page',
  standalone: true,
  imports: [CommonModule, LoginForm],
  templateUrl: './login-page.html',
  styleUrl: './login-page.css'
})
export class LoginPage {
  private authFacade = inject(AuthFacade);

  onLogin(data: LoginRequestDto): void {
    this.authFacade.login(data).subscribe({
      next: () => console.log('Успешный вход'),
      error: (e) => console.error('Ошибка входа', e)
    });
  }
}


