import { Component, inject } from '@angular/core';
import { Router } from '@angular/router';
import { SessionService } from '@app/core/services/session.service';

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
  private sessionService = inject(SessionService);
  private router = inject(Router);

  navigateToProfile(): void {
    this.sessionService.ensureSession().subscribe(() => {
      const userGuid = this.sessionService.userId();
      if (userGuid) {
        this.router.navigate(['/profile', userGuid]);
      } else {
        this.router.navigate(['/auth/login']);
      }
    });
  }
}
