import { Component, inject, input, signal, output, computed, CUSTOM_ELEMENTS_SCHEMA, effect, OnDestroy, TemplateRef, ViewChild, ViewContainerRef } from '@angular/core';
import { Overlay, OverlayRef, OverlayModule } from '@angular/cdk/overlay';
import { TemplatePortal, PortalModule } from '@angular/cdk/portal';
import { PostsFacade } from '@app/features/profile/services/posts-facade';
import { VideoPlayer } from '@app/shared/components/video-player/video-player';
import { AudioPlayer } from '@app/shared/components/audio-player/audio-player';
import { PostComments } from "@app/shared/components/post-comments/post-comments";
import { register } from 'swiper/element/bundle';
import { LucideAngularModule } from 'lucide-angular';

// Регистрируем Swiper элементы
register();

interface PostVideo {
  id: string;
  title: string;
  status: string;
  thumbnailUrl?: string | null;
  hlsUrl?: string | null;
}

interface PostAudio {
  id: string;
  title: string;
  status: string;
  processedPath?: string | null;
}

interface Post {
  id: string;
  title?: string | null;
  text?: string | null;
  createdAt: string;
  publishedAt?: string | null;
  isPublished?: boolean;
  likesCount?: number;
  dislikesCount?: number;
  commentsCount?: number;
  videos?: PostVideo[];
  audios?: PostAudio[];
  pictureUrls?: string[];
  reactionTypeId?: number; // 0 = нет реакции, 1 = лайк, 2 = дизлайк
  isLocked?: boolean;
  lockedMessage?: string | null;
  isShadowBanned?: boolean;
  audienceType?: string | null;
  subscriptionIds?: string[];
}

@Component({
  selector: 'app-post-card',
  imports: [VideoPlayer, AudioPlayer, PostComments, OverlayModule, PortalModule, LucideAngularModule],
  templateUrl: './post-card.html',
  styleUrls: ['./post-card.css'],
  schemas: [CUSTOM_ELEMENTS_SCHEMA],
})
export class PostCard implements OnDestroy {
  readonly post = input.required<Post>();
  readonly selectedVideoIndex = signal(0);
  readonly showComments = signal(false);
  readonly isOwner = input<boolean>(false); 
  readonly deleted = output<string>();
  readonly hidden = output<string>(); // Когда пост скрыт (moved to drafts)
  readonly showEditModal = signal(false);
  readonly editTitle = signal<string>('');
  readonly editText = signal<string>('');
  readonly isUpdating = signal(false);
  readonly editAudienceType = signal<'public' | 'subscribers'>('public');
  readonly editAvailableSubscriptions = signal<any[]>([]);
  readonly editSelectedSubscriptionIds = signal<Set<string>>(new Set<string>());
  readonly editSubscriptionsLoading = signal(false);
  readonly editSubscriptionsError = signal<string | null>(null);
  
  // CDK Overlay для модалок
  @ViewChild('editModalTemplate') editModalTemplate!: TemplateRef<unknown>;
  @ViewChild('deleteModalTemplate') deleteModalTemplate!: TemplateRef<unknown>;
  @ViewChild('hideModalTemplate') hideModalTemplate!: TemplateRef<unknown>;
  @ViewChild('imageModalTemplate') imageModalTemplate!: TemplateRef<unknown>;
  private overlay = inject(Overlay);
  private viewContainerRef = inject(ViewContainerRef);
  private editOverlayRef: OverlayRef | null = null;
  private deleteOverlayRef: OverlayRef | null = null;
  private hideOverlayRef: OverlayRef | null = null;
  private imageOverlayRef: OverlayRef | null = null;
  readonly expandedImageUrl = signal<string | null>(null);
  readonly currentImageIndex = signal<number>(0);
  readonly imageItems = signal<Array<{ url: string }>>([]);
  
  // Локальное состояние для оптимистичного обновления UI
  readonly currentTitle = signal<string | null>(null);
  readonly currentText = signal<string | null>(null);
  
  // Локальное состояние реакций (для оптимистичного обновления UI)
  readonly currentLikesCount = signal<number>(0);
  readonly currentDislikesCount = signal<number>(0);
  readonly currentReaction = signal<number>(0);
  readonly isReacting = signal(false);

  private postsFacade = inject(PostsFacade);

  private previousPostId: string | null = null;

  constructor() {
    // Синхронизируем локальное состояние с входными данными
    effect(() => {
      const post = this.post();
      this.currentLikesCount.set(post.likesCount || 0);
      this.currentDislikesCount.set(post.dislikesCount || 0);
      this.currentReaction.set(post.reactionTypeId || 0);
      
      // Обновляем title и text только если это новый пост (изменился ID)
      if (this.previousPostId !== post.id) {
        this.currentTitle.set(post.title || null);
        this.currentText.set(post.text || null);
        this.previousPostId = post.id;
      }
    });

    // Закрытие модальных окон при клике вне их
    document.addEventListener('click', this.handleDocumentClick);
  }

  ngOnDestroy(): void {
    // Очищаем медиа перед уничтожением компонента
    this.clearMedia();
    document.removeEventListener('click', this.handleDocumentClick);
    if (this.editOverlayRef) {
      this.editOverlayRef.dispose();
    }
    if (this.deleteOverlayRef) {
      this.deleteOverlayRef.dispose();
    }
    if (this.hideOverlayRef) {
      this.hideOverlayRef.dispose();
    }
    if (this.imageOverlayRef) {
      this.imageOverlayRef.dispose();
      this.imageOverlayRef = null;
    }
  }

  openImageModal(imageUrl: string): void {
    // Получаем все изображения из медиа элементов
    const images = this.mediaItems().filter(item => item.type === 'image');
    if (images.length === 0) return;
    
    // Находим индекс выбранного изображения
    const imageIndex = images.findIndex(img => img.url === imageUrl);
    const selectedIndex = imageIndex >= 0 ? imageIndex : 0;
    
    this.imageItems.set(images.map(img => ({ url: img.url })));
    this.currentImageIndex.set(selectedIndex);
    this.expandedImageUrl.set(imageUrl);
    
    // Создаём overlay на весь экран
    this.imageOverlayRef = this.overlay.create({
      hasBackdrop: true,
      backdropClass: 'image-modal-backdrop',
      panelClass: 'image-modal-panel',
      positionStrategy: this.overlay.position().global().centerHorizontally().centerVertically(),
      scrollStrategy: this.overlay.scrollStrategies.block(),
      width: '100vw',
      height: '100vh',
      maxWidth: '100vw',
      maxHeight: '100vh'
    });
    
    // Подключаем template
    const portal = new TemplatePortal(this.imageModalTemplate, this.viewContainerRef);
    this.imageOverlayRef.attach(portal);
    
    // Закрытие по клику на backdrop
    this.imageOverlayRef.backdropClick().subscribe(() => this.closeImageModal());
    
    // Закрытие по нажатию Escape
    this.imageOverlayRef.keydownEvents().subscribe(event => {
      if (event.key === 'Escape') {
        this.closeImageModal();
      } else if (event.key === 'ArrowLeft') {
        this.previousImage();
      } else if (event.key === 'ArrowRight') {
        this.nextImage();
      }
    });
  }

  closeImageModal(): void {
    if (this.imageOverlayRef) {
      this.imageOverlayRef.dispose();
      this.imageOverlayRef = null;
    }
    this.expandedImageUrl.set(null);
    this.currentImageIndex.set(0);
    this.imageItems.set([]);
  }

  previousImage(): void {
    const currentIndex = this.currentImageIndex();
    if (currentIndex > 0) {
      const newIndex = currentIndex - 1;
      this.currentImageIndex.set(newIndex);
      this.expandedImageUrl.set(this.imageItems()[newIndex].url);
    }
  }

  nextImage(): void {
    const currentIndex = this.currentImageIndex();
    const items = this.imageItems();
    if (currentIndex < items.length - 1) {
      const newIndex = currentIndex + 1;
      this.currentImageIndex.set(newIndex);
      this.expandedImageUrl.set(items[newIndex].url);
    }
  }

  private handleDocumentClick = (event: MouseEvent): void => {
    const target = event.target as HTMLElement;
    if (this.showEditModal() && !target.closest('.edit-modal') && !target.closest('button[title="Редактировать пост"]')) {
      this.closeEditModal();
    }
  };

  get currentVideo() {
    const videos = this.post().videos;
    if (!videos || videos.length === 0) return null;

    const video = videos[this.selectedVideoIndex()];

    return video;
  }

  selectVideo(index: number): void {
    this.selectedVideoIndex.set(index);
  }

  getVideoThumbnailUrl(videoId: string): string {
    return this.postsFacade.getVideoThumbnailUrl(videoId);
  }

  getVideoHlsUrl(videoId: string): string {
    return this.postsFacade.getVideoHlsUrl(videoId);
  }

  getPostImageUrl(imagePath: string): string {
    return this.postsFacade.getPostImageUrl(imagePath);
  }

  openDeleteModal(event: Event): void {
    event.stopPropagation();
    
    // Создаём overlay
    this.deleteOverlayRef = this.overlay.create({
      hasBackdrop: true,
      backdropClass: 'delete-modal-backdrop',
      panelClass: 'delete-modal-panel',
      positionStrategy: this.overlay.position().global().centerHorizontally().centerVertically(),
      scrollStrategy: this.overlay.scrollStrategies.block(),
      width: '95vw',
      maxWidth: '400px'
    });
    
    // Подключаем template
    const portal = new TemplatePortal(this.deleteModalTemplate, this.viewContainerRef);
    this.deleteOverlayRef.attach(portal);
    
    // Закрытие по клику на backdrop
    this.deleteOverlayRef.backdropClick().subscribe(() => this.closeDeleteModal());
  }
  
  closeDeleteModal(): void {
    if (this.deleteOverlayRef) {
      this.deleteOverlayRef.dispose();
      this.deleteOverlayRef = null;
    }
  }

  confirmDelete(): void {
    const postId = this.post().id;
    this.postsFacade.deletePost(postId).subscribe({
      next: () => {
        console.log('Пост удален успешно:', postId);
        // Очищаем медиа перед удалением компонента
        this.clearMedia();
        this.closeDeleteModal();
        this.deleted.emit(postId);
      },
      error: (error) => {
        console.error('Ошибка удаления поста:', error);
        this.closeDeleteModal();
      }
    });
  }

  openHideModal(event: Event): void {
    event.stopPropagation();
    
    // Создаём overlay
    this.hideOverlayRef = this.overlay.create({
      hasBackdrop: true,
      backdropClass: 'hide-modal-backdrop',
      panelClass: 'hide-modal-panel',
      positionStrategy: this.overlay.position().global().centerHorizontally().centerVertically(),
      scrollStrategy: this.overlay.scrollStrategies.block(),
      width: '95vw',
      maxWidth: '400px'
    });
    
    // Подключаем template
    const portal = new TemplatePortal(this.hideModalTemplate, this.viewContainerRef);
    this.hideOverlayRef.attach(portal);
    
    // Закрытие по клику на backdrop
    this.hideOverlayRef.backdropClick().subscribe(() => this.closeHideModal());
  }

  closeHideModal(): void {
    if (this.hideOverlayRef) {
      this.hideOverlayRef.dispose();
      this.hideOverlayRef = null;
    }
  }

  confirmHide(): void {
    const postId = this.post().id;
    
    this.postsFacade.unpublishPost(postId).subscribe({
      next: () => {
        // Очищаем медиа перед скрытием компонента
        this.clearMedia();
        this.closeHideModal();
        this.hidden.emit(postId);
      },
      error: (error) => {
        console.error('Ошибка скрытия поста:', error);
        this.closeHideModal();
      }
    });
  }

  private clearMedia(): void {
    // Очищаем медиа, чтобы предотвратить попытки загрузки после удаления/скрытия
    // Это поможет избежать ошибок в audio-player
    // Компонент будет удален из DOM, но перед этим очистим медиа
  }

  openEditModal(event: Event): void {
    event.stopPropagation();
    const post = this.post();
    this.editTitle.set(post.title || '');
    this.editText.set(post.text || '');
    
    // Инициализируем видимость поста
    const audienceType = post.audienceType === 'Subscribers' ? 'subscribers' : 'public';
    this.editAudienceType.set(audienceType);
    this.editSelectedSubscriptionIds.set(new Set(post.subscriptionIds || []));
    
    // Загружаем подписки, если пост не опубликован (черновик)
    const isPostPublished = post.isPublished === true || (post.publishedAt != null && post.publishedAt !== '');
    if (!isPostPublished) {
      this.loadSubscriptionsForEdit();
    }
    
    // Создаём overlay
    this.editOverlayRef = this.overlay.create({
      hasBackdrop: true,
      backdropClass: 'edit-modal-backdrop',
      panelClass: 'edit-modal-panel',
      positionStrategy: this.overlay.position().global().centerHorizontally().centerVertically(),
      scrollStrategy: this.overlay.scrollStrategies.block(),
      width: '95vw',
      maxWidth: '800px'
    });
    
    // Подключаем template
    const portal = new TemplatePortal(this.editModalTemplate, this.viewContainerRef);
    this.editOverlayRef.attach(portal);
    
    // Закрытие по клику на backdrop
    this.editOverlayRef.backdropClick().subscribe(() => this.closeEditModal());
    
    this.showEditModal.set(true);
  }

  loadSubscriptionsForEdit(): void {
    this.editSubscriptionsLoading.set(true);
    this.editSubscriptionsError.set(null);
    
    this.postsFacade.getCreatorSubscriptions().subscribe({
      next: (subscriptions) => {
        this.editAvailableSubscriptions.set(subscriptions);
        this.editSubscriptionsLoading.set(false);
      },
      error: (error) => {
        console.error('Ошибка загрузки подписок:', error);
        this.editSubscriptionsError.set('Не удалось загрузить список подписок');
        this.editSubscriptionsLoading.set(false);
      }
    });
  }

  setEditAudience(type: 'public' | 'subscribers'): void {
    this.editAudienceType.set(type);
    if (type === 'public') {
      this.editSelectedSubscriptionIds.set(new Set());
    }
  }

  toggleEditSubscription(subscriptionId: string): void {
    const current = this.editSelectedSubscriptionIds();
    const updated = new Set(current);
    if (updated.has(subscriptionId)) {
      updated.delete(subscriptionId);
    } else {
      updated.add(subscriptionId);
    }
    this.editSelectedSubscriptionIds.set(updated);
  }

  isEditSubscriptionSelected(subscriptionId: string): boolean {
    return this.editSelectedSubscriptionIds().has(subscriptionId);
  }

  closeEditModal(): void {
    if (this.editOverlayRef) {
      this.editOverlayRef.dispose();
      this.editOverlayRef = null;
    }
    this.showEditModal.set(false);
  }

  saveEdit(): void {
    if (this.isUpdating()) return;
    
    const title = this.editTitle().trim();
    const text = this.editText().trim();
    
    if (!title && !text) {
      return;
    }

    // Сохраняем старое состояние для отката при ошибке
    const oldTitle = this.currentTitle();
    const oldText = this.currentText();

    // Оптимистичное обновление UI
    this.currentTitle.set(title || null);
    this.currentText.set(text || null);

    this.isUpdating.set(true);
    const post = this.post();
    
    // Обновляем текст
    this.postsFacade.updatePostText(post.id, title, text).subscribe({
      next: () => {
        // Если пост не опубликован, обновляем видимость
        // Используем isPublished, если оно есть, иначе проверяем publishedAt
        const isPostPublished = post.isPublished === true || (post.publishedAt != null && post.publishedAt !== '');
        if (!isPostPublished) {
          const selectedSubscriptionIds = this.editSelectedSubscriptionIds();
          const subscriptionIdsArray = selectedSubscriptionIds.size > 0 
            ? Array.from(selectedSubscriptionIds) 
            : null;
          
          // Если выбрано "subscribers", но нет выбранных подписок, не отправляем запрос
          if (this.editAudienceType() === 'subscribers' && (!subscriptionIdsArray || subscriptionIdsArray.length === 0)) {
            console.warn('Нельзя установить видимость "Только подписчики" без выбранных подписок');
            this.closeEditModal();
            this.isUpdating.set(false);
            return;
          }
          
          this.postsFacade.updatePostAudience(
            post.id,
            this.editAudienceType() === 'public' ? true : (this.editAudienceType() === 'subscribers' ? false : null),
            subscriptionIdsArray
          ).subscribe({
            next: () => {
              console.log('Пост обновлен успешно:', post.id);
              this.closeEditModal();
              this.isUpdating.set(false);
            },
            error: (error) => {
              console.error('Ошибка обновления видимости поста:', error);
              this.closeEditModal();
              this.isUpdating.set(false);
            }
          });
        } else {
          console.log('Пост обновлен успешно:', post.id);
          this.closeEditModal();
          this.isUpdating.set(false);
        }
      },
      error: (error) => {
        console.error('Ошибка обновления поста:', error);
        // Откатываем изменения при ошибке
        this.currentTitle.set(oldTitle);
        this.currentText.set(oldText);
        this.isUpdating.set(false);
      }
    });
  }

  // Геттеры для отображения title и text (используют локальное состояние если есть)
  get displayTitle(): string | null {
    return this.currentTitle() ?? this.post().title ?? null;
  }

  get displayText(): string | null {
    if (this.post().isLocked) {
      return null;
    }
    return this.currentText() ?? this.post().text ?? null;
  }

  formatDate(date: string): string {
    return new Date(date).toLocaleDateString('ru-RU', {
      year: 'numeric',
      month: 'long',
      day: 'numeric',
    });
  }

  toggleComments(): void {
    this.showComments.update(show => !show);
  }

  // Обработка реакций
  handleReaction(reactionTypeId: number): void {
    if (this.isReacting()) return; // Предотвращаем множественные клики

    const currentReaction = this.currentReaction();
    const currentLikes = this.currentLikesCount();
    const currentDislikes = this.currentDislikesCount();

    // Сохраняем старое состояние для отката при ошибке
    const oldLikes = currentLikes;
    const oldDislikes = currentDislikes;
    const oldReaction = currentReaction;

    // Определяем новую реакцию
    let newReactionTypeId = reactionTypeId;
    // Если та же реакция - убираем её
    if (currentReaction === reactionTypeId) {
      newReactionTypeId = 0;
    }

    // Оптимистичное обновление UI
    let newLikes = currentLikes;
    let newDislikes = currentDislikes;

    // Убираем старую реакцию
    if (currentReaction === 1) {
      newLikes = Math.max(0, currentLikes - 1);
    } else if (currentReaction === 2) {
      newDislikes = Math.max(0, currentDislikes - 1);
    }

    // Добавляем новую реакцию
    if (newReactionTypeId === 1) {
      newLikes = newLikes + (currentReaction === 1 ? 0 : 1); // Если уже был лайк, не добавляем
    } else if (newReactionTypeId === 2) {
      newDislikes = newDislikes + (currentReaction === 2 ? 0 : 1); // Если уже был дизлайк, не добавляем
    }

    this.currentLikesCount.set(newLikes);
    this.currentDislikesCount.set(newDislikes);
    this.currentReaction.set(newReactionTypeId);
    this.isReacting.set(true);

    this.postsFacade.changeReaction(this.post().id, newReactionTypeId).subscribe({
      next: () => {
        this.isReacting.set(false);
      },
      error: (error) => {
        console.error('Ошибка изменения реакции:', error);
        // Откатываем изменения при ошибке
        this.currentLikesCount.set(oldLikes);
        this.currentDislikesCount.set(oldDislikes);
        this.currentReaction.set(oldReaction);
        this.isReacting.set(false);
      }
    });
  }

  get likeButtonClass(): string {
    const baseClass = 'flex items-center gap-2 px-4 py-2 rounded-lg transition-all font-medium';
    if (this.currentReaction() === 1) {
      return `${baseClass} !bg-red-500 !text-white hover:!bg-red-600`;
    }
    return `${baseClass} text-gray-500 hover:text-red-600 hover:bg-gray-50`;
  }

  get dislikeButtonClass(): string {
    const baseClass = 'flex items-center gap-2 px-4 py-2 rounded-lg transition-all font-medium';
    if (this.currentReaction() === 2) {
      return `${baseClass} !bg-gray-500 !text-white hover:!bg-gray-600`;
    }
    return `${baseClass} text-gray-500 hover:text-gray-700 hover:bg-gray-50`;
  }

  // Получаем все медиа элементы (видео, изображения, аудио) - аудио всегда в конце
  // Используем computed для кеширования результата и предотвращения лишних вычислений
  readonly mediaItems = computed(() => {
    const post = this.post();
    if (post.isLocked) {
      return [];
    }
    const items: Array<{ type: 'video' | 'image' | 'audio'; url: string; videoId?: string; audioId?: string; title?: string; thumbnailUrl?: string | null }> = [];
    const videos = post.videos;
    const audios = post.audios;
    const pictureUrls = post.pictureUrls;
    
    // Сначала добавляем видео
    if (videos && videos.length > 0) {
      videos.forEach(video => {
        if (video.status === 'READY') {
          // Используем hlsUrl из API, если он есть, иначе генерируем
          let hlsUrl = video.hlsUrl || this.getVideoHlsUrl(video.id);
          
          // Нормализуем URL: исправляем регистр /api/files/ -> /api/Files/
          if (hlsUrl && hlsUrl.includes('/api/files/')) {
            hlsUrl = hlsUrl.replace('/api/files/', '/api/Files/');
          }
          
          items.push({
            type: 'video',
            url: hlsUrl,
            videoId: video.id,
            title: video.title,
            thumbnailUrl: video.thumbnailUrl
          });
        }
      });
    }
    
    // Затем добавляем изображения
    if (pictureUrls && pictureUrls.length > 0) {
      pictureUrls.forEach(imageUrl => {
        items.push({
          type: 'image',
          url: this.getPostImageUrl(imageUrl)
        });
      });
    }
    
    // В конце добавляем аудио (всегда под изображениями и видео)
    if (audios && audios.length > 0) {
      audios.forEach(audio => {
        if (audio.status === 'READY' && audio.processedPath) {
          // processedPath уже содержит presigned URL от бэкенда
          // Проверяем, что URL валидный
          const audioUrl = audio.processedPath;
          
          // Проверка на валидность URL - более строгая проверка
          if (!audioUrl || 
              typeof audioUrl !== 'string' ||
              audioUrl.trim() === '' || 
              audioUrl === '/' || 
              audioUrl === 'https://localhost:4200/' ||
              audioUrl === 'http://localhost:4200/' ||
              audioUrl === 'https://donutsbox.ru/' ||
              audioUrl === 'http://donutsbox.ru/' ||
              audioUrl.startsWith('https://localhost:4200/') ||
              audioUrl.startsWith('http://localhost:4200/')) {
            return;
          }
          
          // Убеждаемся, что URL полный (начинается с http:// или https://)
          if (!audioUrl.startsWith('http://') && !audioUrl.startsWith('https://')) {
            return;
          }
          
          // Проверяем, что URL не является базовым URL приложения
          try {
            const url = new URL(audioUrl);
            // Если это базовый URL без пути или с пустым путем, пропускаем
            if (!url.pathname || url.pathname === '/' || url.pathname.trim() === '') {
              return;
            }
          } catch (e) {
            // Если не удалось распарсить URL, пропускаем
            return;
          }
          
          items.push({
            type: 'audio',
            url: audioUrl,
            audioId: audio.id,
            title: audio.title
          });
        }
      });
    }
    
    return items;
  });

  // Определяет, нужно ли показывать медиа списком (вместо карусели)
  // Используем computed для кеширования результата
  readonly shouldShowMediaAsList = computed(() => {
    const items = this.mediaItems();
    if (items.length === 0) return false;
    
    const audioCount = items.filter(item => item.type === 'audio').length;
    const imageCount = items.filter(item => item.type === 'image').length;
    const videoCount = items.filter(item => item.type === 'video').length;
    
    // Показываем списком если:
    // 1. Есть несколько аудио
    // 2. Есть аудио и другие типы медиа (изображения или видео)
    // 3. Есть несколько видео
    // 4. Есть видео и изображения
    return (audioCount > 1) || 
           (audioCount > 0 && (imageCount > 0 || videoCount > 0)) ||
           (videoCount > 1) ||
           (videoCount > 0 && imageCount > 0);
  });

  getAudioUrl(processedPath: string | null | undefined): string {
    if (!processedPath) {
      return '';
    }
    // Формируем URL для аудио через API
    // processedPath имеет формат: processed/{audioId}/audio.mp3
    return `/api/Files/audio/url?key=${encodeURIComponent(processedPath)}&ttl=300`;
  }
}

