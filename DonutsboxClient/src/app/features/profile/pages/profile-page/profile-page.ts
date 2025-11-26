import { Component, inject, OnInit, signal, OnDestroy, ViewChild, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, ActivatedRoute } from '@angular/router';
import { AuthFacade } from '../../../auth/services/auth-facade';
import { AuthorSupporters } from '../../components/author-supporters/author-supporters';
import { CreatePostModal } from '../../components/create-post-modal/create-post-modal';
import { AvatarUploadModal } from '../../components/avatar-upload-modal/avatar-upload-modal';
import { BannerUploadModal } from '../../components/banner-upload-modal/banner-upload-modal';
import { PostsFeed } from '@app/shared/components/posts-feed/posts-feed';
import { UserSubscriptions } from '../../components/user-subscriptions/user-subscriptions';
import { VideoProcessingIndicator } from '../../components/video-processing-indicator/video-processing-indicator';
import { ProfileFacade } from '../../services/profile-facade';
import { PostsFacade } from '../../services/posts-facade';
import { PostsRefresh } from '@app/core/services/posts-refresh.service';
import { UserSubscriptionsFacade } from '../../services/user-subscriptions-facade';
import { SubscriptionModalService } from '@app/shared/services/subscription-modal.service';
import { CreateSubscriptionModalService } from '@app/shared/services/create-subscription-modal.service';
import { AuthorRequestDto, UserDataService } from '@app/api/donutsbox';
import { of, Subscription } from 'rxjs';
import { SessionService } from '@app/core/services/session.service';
import { catchError } from 'rxjs/operators';
import { ChangePasswordModal } from '@app/shared/components/change-password-modal/change-password-modal';
import { ChangeEmailModal } from '@app/shared/components/change-email-modal/change-email-modal';

@Component({
  selector: 'app-profile-page',
  standalone: true,
  imports: [CommonModule, AuthorSupporters, CreatePostModal, AvatarUploadModal, BannerUploadModal, PostsFeed, UserSubscriptions, VideoProcessingIndicator, ChangePasswordModal, ChangeEmailModal],
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
  private userDataService = inject(UserDataService);
  private postsRefresh = inject(PostsRefresh);

  readonly isOwnProfile = signal(false);
  readonly profileId = signal<string | null>(null);  
  readonly isCurrentUserCreator = signal(false);
  readonly showCreatePostModal = signal(false);
  readonly author = signal<AuthorRequestDto | null>(null);
  readonly bannerSrc = signal<string | null>(null);
  readonly avatarSrc = signal<string | null>(null);
  readonly isSubscribed = signal(false);
  readonly showUnsubscribeModal = signal(false);
  readonly isUploadingAvatar = signal(false);
  readonly avatarUploadError = signal<string | null>(null);
  readonly showAvatarModal = signal(false);
  readonly showBannerModal = signal(false);
  
  // Черновики
  readonly showDrafts = signal(false);
  readonly drafts = signal<any[]>([]);
  readonly draftsLoading = signal(false);
  
  // Настройки
  readonly showSettingsDropdown = signal(false);
  readonly showChangePasswordModal = signal(false);
  readonly showChangeEmailModal = signal(false);
  
  @ViewChild(AvatarUploadModal) avatarModal?: AvatarUploadModal;
  @ViewChild(BannerUploadModal) bannerModal?: BannerUploadModal;
  
  private subscriptionSuccessSub?: Subscription;
  private subscriptionCreatedSub?: Subscription;

  // Закрываем dropdown при клике вне (но не закрываем если открыта модалка!)
  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    // Не закрываем dropdown если модалка открыта
    if (this.showChangePasswordModal() || this.showChangeEmailModal()) {
      return;
    }
    
    if (this.showSettingsDropdown()) {
      console.log('📍 Закрываем dropdown при клике вне');
      this.showSettingsDropdown.set(false);
    }
  }


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
      // Загружаем аватарку пользователя для своего профиля
      if (this.isOwnProfile()) {
        this.loadUserAvatar();
      }
    });
    
    this.sessionService.ensureSession().subscribe(() => {
      this.checkProfileOwnership();
      this.checkUserRole();
      // Загружаем аватарку и черновики после проверки сессии
      if (this.isOwnProfile()) {
        this.loadUserAvatar();
        this.loadDraftsIfCreator();
      }
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
      this.avatarSrc.set(null);
      return;
    }

    const session = this.sessionService.session();
    const isOwnNonCreatorProfile =
      !!session && profileId === session.userId && !session.hasCreatorPage;

    if (isOwnNonCreatorProfile) {
      // Logged-in user is not a creator, no author data exists in backend.
      this.author.set(null);
      this.bannerSrc.set(null);
      this.avatarSrc.set(null);
      return;
    }

    this.profileFacade.getAuthorById(profileId).subscribe(author => {
      this.author.set(author);
      
      // Загрузка баннера
      const bannerKey = author?.bannerUrl ?? null;
      if (!bannerKey) {
        this.bannerSrc.set(null);
      } else {
        this.profileFacade.getImageUrl(bannerKey, 300).subscribe({
          next: (url) => this.bannerSrc.set(url),
          error: () => this.bannerSrc.set(null)
        });
      }
      
      // Загрузка аватарки
      const avatarKey = author?.avatarUrl ?? null;
      if (!avatarKey) {
        this.avatarSrc.set(null);
      } else {
        this.profileFacade.getImageUrl(avatarKey, 300).subscribe({
          next: (url) => this.avatarSrc.set(url),
          error: () => this.avatarSrc.set(null)
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

  private loadUserAvatar(): void {
    this.userDataService.apiUserDataMeGet().pipe(
      catchError(() => of(null))
    ).subscribe(userData => {
      if (userData?.avatarUrl) {
        this.profileFacade.getImageUrl(userData.avatarUrl, 300).subscribe({
          next: (url) => this.avatarSrc.set(url),
          error: () => {}
        });
      }
    });
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

  toggleSettingsDropdown(event: Event): void {
    event.stopPropagation();
    event.preventDefault();
    console.log('🔧 Переключение dropdown, текущее состояние:', this.showSettingsDropdown());
    this.showSettingsDropdown.update(v => !v);
    console.log('🔧 Новое состояние dropdown:', this.showSettingsDropdown());
  }

  openChangePasswordModal(event?: Event): void {
    if (event) {
      event.stopPropagation();
      event.preventDefault();
    }
    this.showSettingsDropdown.set(false);
    
    // Небольшая задержка для плавности закрытия dropdown
    setTimeout(() => {
      this.showChangePasswordModal.set(true);
    }, 100);
  }

  closeChangePasswordModal(): void {
    this.showChangePasswordModal.set(false);
  }

  onPasswordChanged(): void {
    // Callback после успешной смены пароля
    // Пользователь остаётся на странице профиля
  }

  openChangeEmailModal(event?: Event): void {
    if (event) {
      event.stopPropagation();
      event.preventDefault();
    }
    this.showSettingsDropdown.set(false);
    
    // Небольшая задержка для плавности закрытия dropdown
    setTimeout(() => {
      this.showChangeEmailModal.set(true);
    }, 100);
  }

  closeChangeEmailModal(): void {
    this.showChangeEmailModal.set(false);
  }

  onEmailChanged(): void {
    // Callback после успешной смены email
    // Пользователь остаётся на странице профиля
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

  openAvatarModal(): void {
    this.showAvatarModal.set(true);
  }

  closeAvatarModal(): void {
    this.showAvatarModal.set(false);
  }

  onAvatarUpload(file: File): void {
    this.avatarModal?.setUploading(true);

    // 1. Загружаем файл в MinIO
    this.profileFacade.uploadAvatar(file).subscribe({
      next: (key) => {
        if (key) {
          // 2. Сохраняем key в UserData.AvatarUrl (backend получает userId из JWT)
          this.profileFacade.updateUserAvatar(key).subscribe({
            next: (success) => {
              if (success) {
                // 3. Получаем URL для отображения
                this.profileFacade.getImageUrl(key, 300).subscribe({
                  next: (url) => {
                    this.avatarSrc.set(url);
                    this.showAvatarModal.set(false);
                    this.avatarModal?.setUploading(false);
                    // Обновляем данные автора если они есть
                    const authorData = this.author();
                    if (authorData) {
                      this.author.set({ ...authorData, avatarUrl: key });
                    }
                  },
                  error: () => {
                    this.avatarModal?.setError('Не удалось загрузить URL аватарки');
                  }
                });
              } else {
                this.avatarModal?.setError('Не удалось сохранить аватарку в БД');
              }
            },
            error: () => {
              this.avatarModal?.setError('Ошибка сохранения аватарки');
            }
          });
        } else {
          this.avatarModal?.setError('Ошибка загрузки');
        }
      },
      error: () => {
        this.avatarModal?.setError('Ошибка загрузки аватарки');
      }
    });
  }

  openBannerModal(): void {
    this.showBannerModal.set(true);
  }

  closeBannerModal(): void {
    this.showBannerModal.set(false);
  }

  onBannerUpload(file: File): void {
    this.bannerModal?.setUploading(true);

    // 1. Загружаем файл в MinIO
    this.profileFacade.uploadBanner(file).subscribe({
      next: (key) => {
        if (key) {
          // 2. Сохраняем key в БД (backend получает userId из JWT)
          this.profileFacade.updateCreatorPageBanner(key).subscribe({
            next: (success) => {
              if (success) {
                // 3. Получаем URL для отображения
                this.profileFacade.getImageUrl(key, 300).subscribe({
                  next: (url) => {
                    this.bannerSrc.set(url);
                    this.showBannerModal.set(false);
                    this.bannerModal?.setUploading(false);
                    // Обновляем данные автора если они есть
                    const authorData = this.author();
                    if (authorData) {
                      this.author.set({ ...authorData, bannerUrl: key });
                    }
                  },
                  error: () => {
                    this.bannerModal?.setError('Не удалось загрузить URL баннера');
                  }
                });
              } else {
                this.bannerModal?.setError('Не удалось сохранить баннер в БД');
              }
            },
            error: () => {
              this.bannerModal?.setError('Ошибка сохранения баннера');
            }
          });
        } else {
          this.bannerModal?.setError('Ошибка загрузки');
        }
      },
      error: () => {
        this.bannerModal?.setError('Ошибка загрузки баннера');
      }
    });
  }

  // Черновики
  loadDraftsIfCreator(): void {
    if (this.isCurrentUserCreator()) {
      this.loadDrafts();
    }
  }

  toggleDrafts(): void {
    if (!this.showDrafts()) {
      this.loadDrafts();
    }
    this.showDrafts.update(v => !v);
  }

  loadDrafts(): void {
    this.draftsLoading.set(true);
    this.postsFacade.getMyPosts(1, 50, false).subscribe({
      next: (response) => {
        this.drafts.set(response.posts || []);
        this.draftsLoading.set(false);
      },
      error: () => {
        this.drafts.set([]);
        this.draftsLoading.set(false);
      }
    });
  }

  publishDraft(postId: string): void {
    this.postsFacade.publishPost(postId).subscribe({
      next: () => {
        // Убираем из черновиков
        this.drafts.update(d => d.filter(p => p.id !== postId));
        // Обновляем ленту
        this.postsRefresh.triggerRefresh();
      },
      error: (err) => {
        console.error('Ошибка публикации:', err);
      }
    });
  }

  onPostHidden(post: any): void {
    // Обновляем статус поста на неопубликованный
    const draftPost = { ...post, isPublished: false };
    // Добавляем пост в начало массива черновиков локально
    this.drafts.update(drafts => [draftPost, ...drafts]);
    // Если черновики не были открыты, открываем их
    if (!this.showDrafts()) {
      this.showDrafts.set(true);
    }
  }

  deleteDraft(postId: string): void {
    if (!confirm('Удалить черновик?')) return;
    
    this.postsFacade.deletePost(postId).subscribe({
      next: () => {
        this.drafts.update(d => d.filter(p => p.id !== postId));
      },
      error: (err) => {
        console.error('Ошибка удаления:', err);
      }
    });
  }
}


