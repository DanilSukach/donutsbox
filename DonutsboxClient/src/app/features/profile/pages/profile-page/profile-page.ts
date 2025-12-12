import { Component, inject, OnInit, signal, OnDestroy, ViewChild, HostListener, TemplateRef, ViewContainerRef, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { Overlay, OverlayRef, OverlayModule } from '@angular/cdk/overlay';
import { TemplatePortal, PortalModule } from '@angular/cdk/portal';
import { AuthFacade } from '../../../auth/services/auth-facade';
import { AuthorSupporters } from '../../components/author-supporters/author-supporters';
import { CreatePostModal } from '../../components/create-post-modal/create-post-modal';
import { AvatarUploadModal } from '../../components/avatar-upload-modal/avatar-upload-modal';
import { BannerUploadModal } from '../../components/banner-upload-modal/banner-upload-modal';
import { PostsFeed } from '@app/shared/components/posts-feed/posts-feed';
import { UserSubscriptions } from '../../components/user-subscriptions/user-subscriptions';
import { CreatorSubscriptions } from '../../components/creator-subscriptions/creator-subscriptions';
import { ProfileFacade } from '../../services/profile-facade';
import { PostsFacade } from '../../services/posts-facade';
import { PostsRefresh } from '@app/core/services/posts-refresh.service';
import { UserSubscriptionsFacade } from '../../services/user-subscriptions-facade';
import { SubscriptionModalService } from '@app/shared/services/subscription-modal.service';
import { CreateSubscriptionModalService } from '@app/shared/services/create-subscription-modal.service';
import { AuthorRequestDto, UserDataService, UserService } from '@app/api/donutsbox';
import { of, Subscription } from 'rxjs';
import { SessionService } from '@app/core/services/session.service';
import { catchError } from 'rxjs/operators';
import { ChangePasswordModal } from '@app/shared/components/change-password-modal/change-password-modal';
import { ChangeEmailModal } from '@app/shared/components/change-email-modal/change-email-modal';
import { FirstLoginModal } from '../../../auth/components/first-login-modal/first-login-modal';
import { VideoStatusPollService } from '../../services/video-status-poll.service';
import { LucideAngularModule } from 'lucide-angular';

@Component({
  selector: 'app-profile-page',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    AuthorSupporters,
    CreatePostModal,
    AvatarUploadModal,
    BannerUploadModal,
    PostsFeed,
    UserSubscriptions,
    CreatorSubscriptions,
    ChangePasswordModal,
    ChangeEmailModal,
    FirstLoginModal,
    OverlayModule,
    PortalModule,
    LucideAngularModule
  ],
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
  private userService = inject(UserService);
  private postsRefresh = inject(PostsRefresh);
  private overlay = inject(Overlay);
  private viewContainerRef = inject(ViewContainerRef);
  private videoStatusPollService = inject(VideoStatusPollService);

  readonly isOwnProfile = signal(false);
  readonly profileId = signal<string | null>(null);  
  readonly isCurrentUserCreator = signal(false);
  readonly showCreatePostModal = signal(false);
  readonly author = signal<AuthorRequestDto | null>(null);
  readonly userName = signal<string | null>(null);
  readonly bannerSrc = signal<string | null>(null);
  readonly bannerLoading = signal(false);
  readonly avatarSrc = signal<string | null>(null);
  
  // Редактирование имени пользователя
  readonly isEditingUserName = signal(false);
  readonly editUserNameValue = signal('');
  readonly isUpdatingUserName = signal(false);
  readonly userNameUpdateError = signal<string | null>(null);
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
  readonly showDeleteDraftModal = signal(false);
  readonly draftToDelete = signal<string | null>(null);
  
  // Редактирование черновика
  readonly editingDraft = signal<any | null>(null);
  readonly editDraftTitle = signal('');
  readonly editDraftText = signal('');
  readonly isUpdatingDraft = signal(false);
  private editDraftOverlayRef: OverlayRef | null = null;
  @ViewChild('editDraftModalTemplate') editDraftModalTemplate!: TemplateRef<any>;
  
  // Настройки
  readonly showSettingsDropdown = signal(false);
  readonly showChangePasswordModal = signal(false);
  readonly showChangeEmailModal = signal(false);
  
  // Мобильное меню
  readonly showMobileMenu = signal(false);
  readonly showMobileSubscriptions = signal(false);
  readonly showMobileSupporters = signal(false);
  readonly showMobileSettings = signal(false);
  
  // Первый вход
  readonly showFirstLoginModal = signal(false);
  
  // Редактирование названия и описания
  readonly isEditingName = signal(false);
  readonly isEditingDescription = signal(false);
  readonly editNameValue = signal('');
  readonly editDescriptionValue = signal('');
  readonly isUpdatingName = signal(false);
  readonly isUpdatingDescription = signal(false);
  readonly nameUpdateError = signal<string | null>(null);
  readonly descriptionUpdateError = signal<string | null>(null);
  
  @ViewChild(AvatarUploadModal) avatarModal?: AvatarUploadModal;
  @ViewChild(BannerUploadModal) bannerModal?: BannerUploadModal;
  
  private subscriptionSuccessSub?: Subscription;
  private subscriptionCreatedSub?: Subscription;
  private postPublishedSub?: Subscription;
  private lastRefreshTrigger = 0;

  constructor() {
    // Подписываемся на обновление постов для обновления черновиков
    effect(() => {
      // Отслеживаем изменения refreshTrigger
      const trigger = this.postsRefresh.refreshTrigger();
      
      // Защита от бесконечного цикла - обновляем только если trigger изменился
      if (trigger > this.lastRefreshTrigger && this.isOwnProfile() && this.isCurrentUserCreator()) {
        this.lastRefreshTrigger = trigger;
        console.log('🔄 Обновление черновиков через effect, trigger:', trigger);
        // Обновляем черновики автоматически при обновлении постов
        this.loadDrafts();
      }
    });
  }

  // Закрываем dropdown при клике вне (но не закрываем если открыта модалка!)
  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    const target = event.target as HTMLElement;
    
    // Не закрываем dropdown если модалка открыта
    if (this.showChangePasswordModal() || this.showChangeEmailModal()) {
      return;
    }
    
    if (this.showSettingsDropdown()) {
      console.log('📍 Закрываем dropdown при клике вне');
      this.showSettingsDropdown.set(false);
    }
    
    // Закрываем мобильное меню при клике вне его
    if (this.showMobileMenu() && !target.closest('.mobile-menu') && !target.closest('button[aria-label="Меню"]')) {
      this.showMobileMenu.set(false);
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
      // Используем queueMicrotask для безопасного обновления после проверки изменений
      queueMicrotask(() => {
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
    });
    
    this.sessionService.ensureSession().subscribe((session) => {
      this.checkProfileOwnership();
      this.checkUserRole();
      // Загружаем аватарку и черновики после проверки сессии
      if (this.isOwnProfile()) {
        this.loadUserAvatar();
        // Загружаем черновики, если пользователь является создателем
        // Используем session?.isCreator напрямую, так как checkUserRole уже установил isCurrentUserCreator
        if (session?.isCreator) {
          this.loadDrafts();
        }
      }
      // Проверяем, нужно ли показать модальное окно первого входа
      if (session?.isFirstLogin && this.isOwnProfile()) {
        this.showFirstLoginModal.set(true);
      }
    });

    // Подписываемся на событие публикации поста через Observable
    this.postPublishedSub = this.postsRefresh.postPublished.subscribe((postId: string) => {
      if (this.isOwnProfile() && this.isCurrentUserCreator()) {
        console.log('🗑️ Получено событие публикации поста:', postId);
        const currentDrafts = this.drafts();
        const beforeCount = currentDrafts.length;
        const normalizedPostId = String(postId).toLowerCase();
        
        console.log('🗑️ Текущие черновики:', currentDrafts.map(d => ({ id: d.id, title: d.title })));
        
        this.drafts.update(d => {
          const filtered = d.filter(p => {
            const draftId = String(p.id || '').toLowerCase();
            const shouldKeep = draftId !== normalizedPostId;
            if (!shouldKeep) {
              console.log('🗑️ Найден пост для удаления:', p.id, '===', postId, 'Title:', p.title);
            }
            return shouldKeep;
          });
          console.log('🗑️ После фильтрации:', filtered.length, 'из', d.length, 'черновиков');
          return filtered;
        });
        
        const afterCount = this.drafts().length;
        console.log('🗑️ Результат удаления:', beforeCount, '->', afterCount, 'черновиков');
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
    this.postPublishedSub?.unsubscribe();
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
    if (this.showDeleteDraftModal()) {
      const target = event.target as HTMLElement;
      // Закрываем модальное окно удаления черновика, если клик был вне его
      if (!target.closest('.delete-draft-modal') && !target.closest('.relative')) {
        this.closeDeleteDraftModal();
      }
    }
  };

  onFirstLoginCompleted(): void {
    this.showFirstLoginModal.set(false);
    // Обновляем сессию после завершения первого входа
    this.sessionService.refreshSession().subscribe(() => {
      // Перезагружаем имя пользователя, чтобы отобразить новое имя
      const profileId = this.profileId();
      if (profileId && this.isOwnProfile()) {
        this.loadUserName(profileId);
      }
    });
  }

  onFirstLoginClosed(): void {
    this.showFirstLoginModal.set(false);
    // Обновляем сессию после закрытия модального окна
    this.sessionService.refreshSession().subscribe(() => {
      // Перезагружаем имя пользователя, чтобы отобразить новое имя (если было введено)
      const profileId = this.profileId();
      if (profileId && this.isOwnProfile()) {
        this.loadUserName(profileId);
      }
    });
  }

  private loadAuthorAndBanner(profileId: string | null): void {
    if (!profileId) {
      this.author.set(null);
      this.userName.set(null);
      this.bannerSrc.set(null);
      this.bannerLoading.set(false);
      this.avatarSrc.set(null);
      return;
    }

    const session = this.sessionService.session();
    const isOwnNonCreatorProfile =
      !!session && profileId === session.userId && !session.hasCreatorPage;

    if (isOwnNonCreatorProfile) {
      // Logged-in user is not a creator, no author data exists in backend.
      // Load user name from User entity
      this.author.set(null);
      this.bannerSrc.set(null);
      this.bannerLoading.set(false);
      this.avatarSrc.set(null);
      this.loadUserName(profileId);
      return;
    }

        this.profileFacade.getAuthorById(profileId).subscribe({
      next: (author) => {
        if (!author) {
          // Автор не найден или в теневом бане - перенаправляем на страницу 404
          this.router.navigate(['/404']);
          return;
        }
        
        this.author.set(author);
        
        // Загружаем имя пользователя из UserData
        if (this.isOwnProfile()) {
          this.loadUserName(profileId);
        }
        
        // Загрузка баннера
        const bannerKey = author?.bannerUrl ?? null;
        if (!bannerKey) {
          this.bannerSrc.set(null);
          this.bannerLoading.set(false);
        } else {
          this.bannerLoading.set(true);
          this.profileFacade.getImageUrl(bannerKey, 300).subscribe({
            next: (url) => {
              this.bannerSrc.set(url);
              this.bannerLoading.set(false);
            },
            error: () => {
              this.bannerSrc.set(null);
              this.bannerLoading.set(false);
            }
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
      },
      error: (error) => {
        // Если получили 404 ошибку, перенаправляем на страницу 404
        if (error?.status === 404) {
          this.router.navigate(['/404']);
        }
      }
    });
  }

  onPostPublished(): void {
    this.showCreatePostModal.set(false);
    // Сразу загружаем черновики и показываем их
    if (this.isCurrentUserCreator()) {
      this.loadDrafts();
      // Раскрываем список черновиков
      this.showDrafts.set(true);
    }
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
    this.bannerLoading.set(false);
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
    this.bannerLoading.set(true);

    // 1. Загружаем файл в MinIO
    this.profileFacade.uploadBanner(file).subscribe({
      next: (key) => {
        if (key) {
          // 2. Сохраняем key в БД (backend получает userId из JWT)
          this.profileFacade.updateCreatorPageBanner(key).subscribe({
            next: (success) => {
              if (success) {
                this.showBannerModal.set(false);
                this.bannerModal?.setUploading(false);
                // Перезагружаем данные автора, чтобы получить актуальный URL баннера
                const profileId = this.profileId();
                if (profileId) {
                  this.loadAuthorAndBanner(profileId);
                }
              } else {
                this.bannerLoading.set(false);
                this.bannerModal?.setError('Не удалось сохранить баннер в БД');
              }
            },
            error: () => {
              this.bannerLoading.set(false);
              this.bannerModal?.setError('Ошибка сохранения баннера');
            }
          });
        } else {
          this.bannerLoading.set(false);
          this.bannerModal?.setError('Ошибка загрузки');
        }
      },
      error: () => {
        this.bannerLoading.set(false);
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
    // Черновики уже загружены при инициализации, просто показываем/скрываем
    this.showDrafts.update(v => !v);
  }

  loadDrafts(): void {
    if (!this.isCurrentUserCreator()) {
      console.log('⚠️ loadDrafts: Пользователь не является создателем');
      return;
    }
    
    console.log('📝 Загрузка черновиков...');
    this.draftsLoading.set(true);
    this.postsFacade.getMyPosts(1, 50, false).subscribe({
      next: (response) => {
        const drafts = response.posts || [];
        console.log('✅ Черновики загружены:', drafts.length, 'постов');
        this.drafts.set(drafts);
        this.draftsLoading.set(false);
        
        // Проверяем, есть ли черновики с уже обработанным медиа, которые должны быть опубликованы
        this.checkAndPublishReadyDrafts(drafts);
        
        // Если есть черновики с обрабатываемым медиа, инициализируем SignalR для отслеживания
        const hasProcessingMedia = drafts.some(d => this.hasProcessingMedia(d));
        if (hasProcessingMedia) {
          console.log('🔄 Найдены черновики с обрабатываемым медиа, инициализирую SignalR...');
          this.videoStatusPollService.startPollingAfterPublish();
        }
      },
      error: (err) => {
        console.error('❌ Ошибка загрузки черновиков:', err);
        this.drafts.set([]);
        this.draftsLoading.set(false);
      }
    });
  }

  private checkAndPublishReadyDrafts(drafts: any[]): void {
    // Проверяем каждый черновик на готовность к публикации
    drafts.forEach(draft => {
      const videos = draft.videos || [];
      const audios = draft.audios || [];
      
      // Проверяем, есть ли медиа
      const hasMedia = videos.length > 0 || audios.length > 0;
      
      if (hasMedia) {
        // Проверяем, все ли медиа обработано
        const hasProcessingVideos = videos.some((v: any) => 
          v.status === 'UPLOADED' || v.status === 'PROCESSING' || v.status === 'UPLOADING');
        const hasProcessingAudios = audios.some((a: any) => 
          a.status === 'UPLOADED' || a.status === 'PROCESSING' || a.status === 'UPLOADING');
        
        // Если есть медиа, но все уже обработано (READY), значит пост должен был быть опубликован
        // но по какой-то причине не был (например, SignalR не успел подключиться)
        if (!hasProcessingVideos && !hasProcessingAudios) {
          const allReady = videos.every((v: any) => v.status === 'READY') && 
                          audios.every((a: any) => a.status === 'READY');
          
          if (allReady && videos.length + audios.length > 0) {
            console.log('✅ Найден черновик с готовым медиа, который должен быть опубликован:', draft.id);
            // Пост должен был быть опубликован автоматически, но не был
            // Обновляем черновики через небольшую задержку, чтобы получить актуальный статус
            setTimeout(() => {
              this.postsRefresh.triggerRefresh();
            }, 1000);
          }
        }
      }
    });
  }

  hasProcessingMedia(draft: any): boolean {
    const videos = draft.videos || [];
    const audios = draft.audios || [];
    
    const hasProcessingVideos = videos.some((v: any) => 
      v.status === 'UPLOADED' || v.status === 'PROCESSING' || v.status === 'UPLOADING');
    const hasProcessingAudios = audios.some((a: any) => 
      a.status === 'UPLOADED' || a.status === 'PROCESSING' || a.status === 'UPLOADING');
    
    return hasProcessingVideos || hasProcessingAudios;
  }

  publishDraft(postId: string): void {
    // Находим черновик
    const draft = this.drafts().find(d => d.id === postId);
    if (!draft) return;
    
    // Проверяем, есть ли необработанное медиа
    if (this.hasProcessingMedia(draft)) {
      console.log('Нельзя опубликовать пост с необработанным медиа');
      return;
    }
    
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
    this.draftToDelete.set(postId);
    this.showDeleteDraftModal.set(true);
  }

  confirmDeleteDraft(): void {
    const postId = this.draftToDelete();
    if (!postId) return;
    
    this.postsFacade.deletePost(postId).subscribe({
      next: () => {
        this.drafts.update(d => d.filter(p => p.id !== postId));
        this.closeDeleteDraftModal();
      },
      error: (err) => {
        console.error('Ошибка удаления:', err);
        this.closeDeleteDraftModal();
      }
    });
  }

  closeDeleteDraftModal(): void {
    this.showDeleteDraftModal.set(false);
    this.draftToDelete.set(null);
  }

  // Загрузка имени пользователя
  private loadUserName(userId: string): void {
    this.userService.apiUserIdGet(userId).subscribe({
      next: (user) => {
        this.userName.set(user.name);
        this.editUserNameValue.set(user.name || '');
      },
      error: (err) => {
        console.error('Ошибка загрузки имени пользователя:', err);
        this.userName.set(null);
      }
    });
  }

  // Редактирование имени пользователя
  startEditingUserName(): void {
    const currentName = this.userName() || '';
    this.editUserNameValue.set(currentName);
    this.userNameUpdateError.set(null);
    this.isEditingUserName.set(true);
  }

  cancelEditingUserName(): void {
    this.isEditingUserName.set(false);
    this.editUserNameValue.set('');
    this.userNameUpdateError.set(null);
  }

  saveUserName(): void {
    const newName = this.editUserNameValue().trim();
    if (!newName) return;

    this.isUpdatingUserName.set(true);
    this.userNameUpdateError.set(null);

    this.profileFacade.updateUserName(newName).subscribe({
      next: (result) => {
        this.isUpdatingUserName.set(false);
        if (result.success) {
          this.userName.set(newName);
          this.isEditingUserName.set(false);
        } else {
          this.userNameUpdateError.set(result.message || 'Ошибка при обновлении имени');
        }
      },
      error: (err) => {
        this.isUpdatingUserName.set(false);
        this.userNameUpdateError.set('Ошибка при обновлении имени');
      }
    });
  }

  openEditDraftModal(draft: any): void {
    this.editingDraft.set(draft);
    this.editDraftTitle.set(draft.title || '');
    this.editDraftText.set(draft.text || '');
    
    // Создаём overlay
    this.editDraftOverlayRef = this.overlay.create({
      hasBackdrop: true,
      backdropClass: 'edit-modal-backdrop',
      panelClass: 'edit-modal-panel',
      positionStrategy: this.overlay.position().global().centerHorizontally().centerVertically(),
      scrollStrategy: this.overlay.scrollStrategies.block(),
      width: '95vw',
      maxWidth: '800px'
    });
    
    // Подключаем template
    const portal = new TemplatePortal(this.editDraftModalTemplate, this.viewContainerRef);
    this.editDraftOverlayRef.attach(portal);
    
    // Закрытие по клику на backdrop
    this.editDraftOverlayRef.backdropClick().subscribe(() => this.closeEditDraftModal());
  }

  closeEditDraftModal(): void {
    if (this.editDraftOverlayRef) {
      this.editDraftOverlayRef.dispose();
      this.editDraftOverlayRef = null;
    }
    this.editingDraft.set(null);
  }

  saveDraftEdit(): void {
    if (this.isUpdatingDraft()) return;
    
    const draft = this.editingDraft();
    if (!draft) return;
    
    const title = this.editDraftTitle().trim();
    const text = this.editDraftText().trim();
    
    if (!title && !text) {
      return;
    }

    this.isUpdatingDraft.set(true);

    this.postsFacade.updatePostText(draft.id, title, text).subscribe({
      next: () => {
        // Обновляем черновик локально
        this.drafts.update(drafts => 
          drafts.map(d => d.id === draft.id ? { ...d, title, text } : d)
        );
        this.isUpdatingDraft.set(false);
        this.closeEditDraftModal();
      },
      error: (err: any) => {
        console.error('Ошибка обновления черновика:', err);
        this.isUpdatingDraft.set(false);
      }
    });
  }

  // Редактирование названия страницы
  startEditingName(): void {
    const currentName = this.author()?.pageName || '';
    this.editNameValue.set(currentName);
    this.nameUpdateError.set(null);
    this.isEditingName.set(true);
  }

  cancelEditingName(): void {
    this.isEditingName.set(false);
    this.editNameValue.set('');
    this.nameUpdateError.set(null);
  }

  saveAuthorName(): void {
    const newName = this.editNameValue().trim();
    if (!newName) return;

    this.isUpdatingName.set(true);
    this.nameUpdateError.set(null);

    this.profileFacade.updateAuthorName(newName).subscribe(result => {
      this.isUpdatingName.set(false);
      
      if (result.success) {
        // Обновляем локально
        const currentAuthor = this.author();
        if (currentAuthor) {
          this.author.set({ ...currentAuthor, pageName: newName });
        }
        this.isEditingName.set(false);
      } else {
        this.nameUpdateError.set(result.message || 'Ошибка при обновлении названия');
      }
    });
  }

  // Редактирование описания
  startEditingDescription(): void {
    const currentDescription = this.author()?.description || '';
    this.editDescriptionValue.set(currentDescription);
    this.descriptionUpdateError.set(null);
    this.isEditingDescription.set(true);
  }

  cancelEditingDescription(): void {
    this.isEditingDescription.set(false);
    this.editDescriptionValue.set('');
    this.descriptionUpdateError.set(null);
  }

  saveAuthorDescription(): void {
    const newDescription = this.editDescriptionValue().trim();

    this.isUpdatingDescription.set(true);
    this.descriptionUpdateError.set(null);

    this.profileFacade.updateAuthorDescription(newDescription).subscribe(result => {
      this.isUpdatingDescription.set(false);
      
      if (result.success) {
        // Обновляем локально
        const currentAuthor = this.author();
        if (currentAuthor) {
          this.author.set({ ...currentAuthor, description: newDescription });
        }
        this.isEditingDescription.set(false);
      } else {
        this.descriptionUpdateError.set(result.message || 'Ошибка при обновлении описания');
      }
    });
  }
}


