import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, ActivatedRoute } from '@angular/router';
import { AuthFacade } from '../../../auth/services/auth-facade';
import { TokenService } from '@app/core/services/token.service';
import { JwtDecodeService } from '@app/core/services/jwt-decode.service';
import { AuthorSupporters } from '../../components/author-supporters/author-supporters';

@Component({
  selector: 'app-profile-page',
  standalone: true,
  imports: [CommonModule, AuthorSupporters],
  templateUrl: './profile-page.html',
  styleUrl: './profile-page.css'
})
export class ProfilePage implements OnInit {
  private authFacade = inject(AuthFacade);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private tokenService = inject(TokenService);
  private jwtService = inject(JwtDecodeService);

  readonly isOwnProfile = signal(false);
  readonly profileId = signal<string | null>(null);

  ngOnInit(): void {
    this.checkProfileOwnership();
  }

  private checkProfileOwnership(): void {
    const profileId = this.route.snapshot.paramMap.get('id');
    const token = this.tokenService.getAccessToken();
    const currentUserGuid = this.jwtService.getGuid(token);

    this.profileId.set(profileId);

    if (profileId && currentUserGuid && profileId === currentUserGuid) {
      this.isOwnProfile.set(true);
    }
  }

  onLogout(): void {
    this.authFacade.logout();
  }

  navigateToFeed(): void {
    console.log('Попытка перехода к ленте...');
    this.router.navigate(['/feed']).then(
      (success) => {
        console.log('Навигация к /feed:', success ? 'успешна' : 'неуспешна');
      },
      (error) => {
        console.error('Ошибка навигации к /feed:', error);
      }
    );
  }
}


