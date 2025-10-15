import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, ActivatedRoute } from '@angular/router';
import { AuthFacade } from '../../../auth/services/auth-facade';
import { TokenService } from '@app/core/services/token.service';
import { JwtDecodeService } from '@app/core/services/jwt-decode.service';
import { AuthorSupporters } from '../../components/author-supporters/author-supporters';
import { CreatePostModal } from '../../components/create-post-modal/create-post-modal';
import { PostsList } from "../../components/posts-list/posts-list";

@Component({
  selector: 'app-profile-page',
  standalone: true,
  imports: [CommonModule, AuthorSupporters, CreatePostModal, PostsList],
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
  readonly isCurrentUserCreator = signal(false);
   readonly showCreatePostModal = signal(false);

  ngOnInit(): void {
    this.checkProfileOwnership();
    this.checkUserRole();
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

  private checkUserRole(): void {
    const token = this.tokenService.getAccessToken();
    const isCreator = this.jwtService.isCreator(token);
    this.isCurrentUserCreator.set(isCreator);
  }

  onAddContent(): void {
    this.showCreatePostModal.set(true);  
  }

  closeCreatePostModal(): void {
    this.showCreatePostModal.set(false);  
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


