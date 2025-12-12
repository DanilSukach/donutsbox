import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { SessionService } from '@app/core/services/session.service';
import { UserDataService, FilesService } from '@app/api/donutsbox';
import { catchError, switchMap } from 'rxjs/operators';
import { of } from 'rxjs';
import { LucideAngularModule } from 'lucide-angular';

@Component({
  selector: 'app-user-profile-icon',
  standalone: true,
  imports: [CommonModule, LucideAngularModule],
  template: `
    <button 
      (click)="navigateToProfile()" 
      class="w-10 h-10 rounded-full flex items-center justify-center hover:opacity-80 transition-opacity overflow-hidden"
      [class.bg-amber-600]="!avatarUrl()"
      title="Перейти в профиль"
    >
      @if (avatarUrl()) {
        <img 
          [src]="avatarUrl()" 
          alt="Аватар"
          class="w-full h-full object-cover"
        />
      } @else {
        <lucide-icon name="user" class="w-6 h-6 text-white" />
      }
    </button>
  `
})
export class UserProfileIcon implements OnInit {
  private sessionService = inject(SessionService);
  private router = inject(Router);
  private userDataService = inject(UserDataService);
  private filesService = inject(FilesService);

  readonly avatarUrl = signal<string | null>(null);

  ngOnInit(): void {
    this.loadAvatar();
  }

  private loadAvatar(): void {
    this.userDataService.apiUserDataMeGet().pipe(
      switchMap(userData => {
        if (userData?.avatarUrl) {
          return this.filesService.apiFilesImagesUrlGet(userData.avatarUrl, 300).pipe(
            catchError(() => of(null))
          );
        }
        return of(null);
      }),
      catchError(() => of(null))
    ).subscribe(response => {
      if (response?.url) {
        this.avatarUrl.set(response.url);
      }
    });
  }

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
