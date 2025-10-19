import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, ActivatedRoute } from '@angular/router';
import { AuthFacade } from '../../../auth/services/auth-facade';
import { TokenService } from '@app/core/services/token.service';
import { JwtDecodeService } from '@app/core/services/jwt-decode.service';
import { AuthorSupporters } from '../../components/author-supporters/author-supporters';
import { CreatePostModal } from '../../components/create-post-modal/create-post-modal';
import { PostsFeed } from '@app/shared/components/posts-feed/posts-feed';
import { UserSubscriptions } from '../../components/user-subscriptions/user-subscriptions';
import { VideoProcessingIndicator } from '../../components/video-processing-indicator/video-processing-indicator';
import { ProfileFacade } from '../../services/profile-facade';
import { PostsFacade } from '../../services/posts-facade';
import { AuthorRequestDto } from '@app/api/donutsbox';

@Component({
  selector: 'app-profile-page',
  standalone: true,
  imports: [CommonModule, AuthorSupporters, CreatePostModal, PostsFeed, UserSubscriptions, VideoProcessingIndicator],
  templateUrl: './profile-page.html',
  styleUrl: './profile-page.css'
})
export class ProfilePage implements OnInit {
   private authFacade = inject(AuthFacade);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private tokenService = inject(TokenService);
  private jwtService = inject(JwtDecodeService);
  private profileFacade = inject(ProfileFacade);
  private postsFacade = inject(PostsFacade);

  readonly isOwnProfile = signal(false);
  readonly profileId = signal<string | null>(null);  
  readonly isCurrentUserCreator = signal(false);
  readonly showCreatePostModal = signal(false);
  readonly author = signal<AuthorRequestDto | null>(null);
  readonly bannerSrc = signal<string | null>(null);

  // Функция для загрузки постов creator'а
  readonly loadCreatorPosts = (page: number, pageSize: number) => {
    const id = this.profileId();
    if (!id) throw new Error('No creator ID');
    return this.postsFacade.getCreatorPosts(id, page, pageSize);
  };

  ngOnInit(): void {
    this.route.paramMap.subscribe(params => {
      const profileId = params.get('id');
      this.profileId.set(profileId);
      this.checkProfileOwnership();
      this.loadAuthorAndBanner(profileId);
    });
    
    this.checkUserRole();
  }

  private loadAuthorAndBanner(profileId: string | null): void {
    if (!profileId) {
      this.author.set(null);
      this.bannerSrc.set(null);
      return;
    }

    this.profileFacade.getAuthorById(profileId).subscribe(author => {
      this.author.set(author);
      const key = author?.bannerUrl ?? null;
      if (!key) {
        this.bannerSrc.set(null);
        return;
      }
      this.profileFacade.getImageUrl(key, 300).subscribe({
        next: (url) => this.bannerSrc.set(url),
        error: () => this.bannerSrc.set(null)
      });
    });
  }

  onPostPublished(): void {
    this.showCreatePostModal.set(false);
  }

  private checkProfileOwnership(): void {
    const profileId = this.profileId();
    const token = this.tokenService.getAccessToken();
    const currentUserGuid = this.jwtService.getGuid(token);

    if (profileId && currentUserGuid && profileId === currentUserGuid) {
      this.isOwnProfile.set(true);
    } else {
      this.isOwnProfile.set(false);
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

  onBannerError(e: Event): void {
    const img = e.target as HTMLImageElement;
    img.src = '/images/banner-placeholder.jpg';
  }
}


