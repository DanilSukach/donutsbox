import { Component, inject } from '@angular/core';
import { Router } from '@angular/router';
import { TokenService } from '@app/core/services/token.service';
import { JwtDecodeService } from '@app/core/services/jwt-decode.service';

@Component({
  selector: 'app-user-profile-icon',
  standalone: true,
  template: `
    <button 
      (click)="navigateToProfile()" 
      class="w-10 h-10 bg-amber-600 rounded-full flex items-center justify-center hover:bg-amber-700 transition-colors"
      title="Перейти в профиль"
    >
      <span class="text-white font-bold">👤</span>
    </button>
  `
})
export class UserProfileIcon {
  private tokenService = inject(TokenService);
  private jwtService = inject(JwtDecodeService);
  private router = inject(Router);

  navigateToProfile(): void {
    const token = this.tokenService.getAccessToken();
    const userGuid = this.jwtService.getGuid(token);
    
    if (userGuid) {
      this.router.navigate(['/profile', userGuid]);
    } else {
      this.router.navigate(['/auth/login']);
    }
  }
}
