import { Component, inject, OnInit, signal, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, ActivatedRoute } from '@angular/router';
import { AuthFacade } from '../../../auth/services/auth-facade';
import { AuthorSupporters } from '../../components/author-supporters/author-supporters';
import { CreatePostModal } from '../../components/create-post-modal/create-post-modal';
import { PostsFeed } from '@app/shared/components/posts-feed/posts-feed';
import { UserSubscriptions } from '../../components/user-subscriptions/user-subscriptions';
import { VideoProcessingIndicator } from '../../components/video-processing-indicator/video-processing-indicator';
import { ProfileFacade } from '../../services/profile-facade';
import { PostsFacade } from '../../services/posts-facade';
import { UserSubscriptionsFacade } from '../../services/user-subscriptions-facade';
import { SubscriptionModalService } from '@app/shared/services/subscription-modal.service';
import { CreateSubscriptionModalService } from '@app/shared/services/create-subscription-modal.service';
import { AuthorRequestDto } from '@app/api/donutsbox';
import { of, Subscription } from 'rxjs';
import { SessionService } from '@app/core/services/session.service';

@Component({
  selector: 'app-profile-page',
  standalone: true,
  imports: [CommonModule, AuthorSupporters, CreatePostModal, PostsFeed, UserSubscriptions, VideoProcessingIndicator],
  templateUrl: './profile-page.html',
  styleUrl: './profile-page.css'
})
export class ProfilePage implements OnInit, OnDestroy {
   private authFacade = inject(AuthFacade);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private sessionService = inject(SessionService);
  private profileFacade = inject(ProfileFacade);
  private postsFacade = inject(PostsFacade);
  private userSubscriptionsFacade = inject(UserSubscriptionsFacade);
  private subscriptionModalService = inject(SubscriptionModalService);
  private createSubscriptionModalService = inject(CreateSubscriptionModalService);

  readonly isOwnProfile = signal(false);
  readonly profileId = signal<string | null>(null);  
  readonly isCurrentUserCreator = signal(false);
  readonly showCreatePostModal = signal(false);
  readonly author = signal<AuthorRequestDto | null>(null);
  readonly bannerSrc = signal<string | null>(null);
  readonly isSubscribed = signal(false);
  readonly showUnsubscribeModal = signal(false);
  
  private subscriptionSuccessSub?: Subscription;
  private subscriptionCreatedSub?: Subscription;

  // Функция для загрузки постов creator'а
  readonly loadCreatorPosts = (page: number, pageSize: number) => {
    const id = this.profileId();
    const authorData = this.author();
    if (!id || !authorData) {
      // Если пользователь не является автором, возвращаем пустой результат
      return of({ 
        creator: undefined, 
        total: 0, 
        page: page, 
        pageSize: pageSize, 
        posts: [] 
      });
    }
    return this.postsFacade.getCreatorPosts(id, page, pageSize);
  };

  ngOnInit(): void {
    this.route.paramMap.subscribe(params => {
      const profileId = params.get('id');
      this.profileId.set(profileId);
      this.checkProfileOwnership();
      this.loadAuthorAndBanner(profileId);
      if (!this.isOwnProfile()) {
        this.loadSubscriptions();
      }
    });
    
    this.sessionService.ensureSession().subscribe(() => {
      this.checkProfileOwnership();
      this.checkUserRole();
    });
    
    // Подписываемся на успешную подписку
    this.subscriptionSuccessSub = this.subscriptionModalService.subscriptionSuccess.subscribe(() => {
      this.loadSubscriptions();
    });

    this.subscriptionCreatedSub = this.createSubscriptionModalService.subscriptionCreated.subscribe(() => {
      const profileId = this.profileId();
      if (profileId) {
        this.loadAuthorAndBanner(profileId);
      }
    });

    // Закрытие модального окна отписки при клике вне его
    document.addEventListener('click', this.handleDocumentClick);
  }

  ngOnDestroy(): void {
    this.subscriptionSuccessSub?.unsubscribe();
    this.subscriptionCreatedSub?.unsubscribe();
    document.removeEventListener('click', this.handleDocumentClick);
  }

  private handleDocumentClick = (event: MouseEvent): void => {
    if (this.showUnsubscribeModal()) {
      const target = event.target as HTMLElement;
      // Закрываем модальное окно, если клик был вне его и вне кнопки подписки
      if (!target.closest('.unsubscribe-modal') && !target.closest('.relative')) {
        this.closeUnsubscribeModal();
      }
    }
  };

  private loadAuthorAndBanner(profileId: string | null): void {
    if (!profileId) {
      this.author.set(null);
      this.bannerSrc.set(null);
      return;
    }

    const session = this.sessionService.session();
    const isOwnNonCreatorProfile =
      !!session && profileId === session.userId && !session.hasCreatorPage;

    if (isOwnNonCreatorProfile) {
      // Logged-in user is not a creator, no author data exists in backend.
      this.author.set(null);
      this.bannerSrc.set(null);
      return;
    }

    this.profileFacade.getAuthorById(profileId).subscribe(author => {
      this.author.set(author);
      const key = author?.bannerUrl ?? null;
      if (!key) {
        this.bannerSrc.set(null);
      } else {
        this.profileFacade.getImageUrl(key, 300).subscribe({
          next: (url) => this.bannerSrc.set(url),
          error: () => this.bannerSrc.set(null)
        });
      }
      
      // Проверяем подписку после загрузки автора
      if (!this.isOwnProfile()) {
        this.checkSubscriptionStatus();
      }
    });
  }

  onPostPublished(): void {
    this.showCreatePostModal.set(false);
  }

  private checkProfileOwnership(): void {
    const profileId = this.profileId();
    const currentUserGuid = this.sessionService.userId();

    if (profileId && currentUserGuid && profileId === currentUserGuid) {
      this.isOwnProfile.set(true);
    } else {
      this.isOwnProfile.set(false);
    }
  }

  private checkUserRole(): void {
    this.isCurrentUserCreator.set(this.sessionService.isCreator());
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

  private loadSubscriptions(): void {
    this.userSubscriptionsFacade.loadUserSubscriptions().subscribe(() => {
      this.checkSubscriptionStatus();
    });
  }

  private checkSubscriptionStatus(): void {
    const profileId = this.profileId();
    if (!profileId) {
      this.isSubscribed.set(false);
      return;
    }

    const subscriptions = this.userSubscriptionsFacade.subscriptions();
    const isSubscribed = subscriptions.some(sub => sub.id === profileId);
    this.isSubscribed.set(isSubscribed);
  }

  onSubscribe(): void {
    const author = this.author();
    if (author) {
      this.subscriptionModalService.open(author);
    }
  }

  openCreateSubscriptionModal(): void {
    this.createSubscriptionModalService.open();
  }

  openUnsubscribeModal(): void {
    this.showUnsubscribeModal.set(true);
  }

  closeUnsubscribeModal(): void {
    this.showUnsubscribeModal.set(false);
  }

  confirmUnsubscribe(): void {
    const profileId = this.profileId();
    if (profileId) {
      this.userSubscriptionsFacade.unsubscribeFromCreator(profileId).subscribe({
        next: () => {
          this.closeUnsubscribeModal();
          this.isSubscribed.set(false);
          this.author.update(author => {
            if (!author) {
              return author;
            }
            const updatedCount = Math.max(0, (author.subscribersCount ?? 0) - 1);
            return { ...author, subscribersCount: updatedCount };
          });
        },
        error: (error) => {
          console.error('Ошибка отписки от создателя:', error);
        }
      });
    }
  }

  formatSubscribersCount(count?: number): string {
    if (!count) return '0';
    if (count >= 1000000) return (count / 1000000).toFixed(1) + 'M';
    if (count >= 1000) return (count / 1000).toFixed(1) + 'K';
    return count.toString();
  }
}


