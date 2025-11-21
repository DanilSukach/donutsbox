import { Component, inject, input, signal, CUSTOM_ELEMENTS_SCHEMA, effect, OnDestroy } from '@angular/core';
import { PostsFacade } from '@app/features/profile/services/posts-facade';
import { VideoPlayer } from '@app/shared/components/video-player/video-player';
import { PostComments } from "@app/shared/components/post-comments/post-comments";
import { register } from 'swiper/element/bundle';

// Регистрируем Swiper элементы
register();

interface PostVideo {
  id: string;
  title: string;
  status: string;
  thumbnailUrl?: string | null;
  hlsUrl?: string | null;
}

interface Post {
  id: string;
  title?: string | null;
  text?: string | null;
  createdAt: string;
  publishedAt?: string | null;
  likesCount?: number;
  dislikesCount?: number;
  commentsCount?: number;
  videos?: PostVideo[];
  pictureUrls?: string[];
  reactionTypeId?: number; // 0 = нет реакции, 1 = лайк, 2 = дизлайк
}

@Component({
  selector: 'app-post-card',
  imports: [VideoPlayer, PostComments],
  templateUrl: './post-card.html',
  styleUrls: ['./post-card.css'],
  schemas: [CUSTOM_ELEMENTS_SCHEMA],
})
export class PostCard implements OnDestroy {
  readonly post = input.required<Post>();
  readonly selectedVideoIndex = signal(0);
  readonly showComments = signal(false);
  readonly isOwner = input<boolean>(false); 
  readonly showDeleteModal = signal(false);
  readonly showEditModal = signal(false);
  readonly editTitle = signal<string>('');
  readonly editText = signal<string>('');
  readonly isUpdating = signal(false);
  
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
    document.removeEventListener('click', this.handleDocumentClick);
  }

  private handleDocumentClick = (event: MouseEvent): void => {
    const target = event.target as HTMLElement;
    if (this.showEditModal() && !target.closest('.edit-modal') && !target.closest('button[title="Редактировать пост"]')) {
      this.closeEditModal();
    }
    if (this.showDeleteModal() && !target.closest('.delete-modal') && !target.closest('button[title="Удалить пост"]')) {
      this.closeDeleteModal();
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
    this.showDeleteModal.set(true);
  }
  
  closeDeleteModal(): void {
    this.showDeleteModal.set(false);
  }

  confirmDelete(): void {
    this.postsFacade.deletePost(this.post().id).subscribe({
      next: () => {
        console.log('Пост удален успешно:', this.post().id);
        this.closeDeleteModal();
      },
      error: (error) => {
        console.error('Ошибка удаления поста:', error);
        this.closeDeleteModal();
      }
    });
  }

  openEditModal(event: Event): void {
    event.stopPropagation();
    const post = this.post();
    this.editTitle.set(post.title || '');
    this.editText.set(post.text || '');
    this.showEditModal.set(true);
  }

  closeEditModal(): void {
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
    this.postsFacade.updatePostText(this.post().id, title, text).subscribe({
      next: () => {
        console.log('Пост обновлен успешно:', this.post().id);
        this.closeEditModal();
        this.isUpdating.set(false);
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
      return `${baseClass} bg-red-50 text-red-600 hover:bg-red-100`;
    }
    return `${baseClass} text-gray-500 hover:text-red-600 hover:bg-gray-50`;
  }

  get dislikeButtonClass(): string {
    const baseClass = 'flex items-center gap-2 px-4 py-2 rounded-lg transition-all font-medium';
    if (this.currentReaction() === 2) {
      return `${baseClass} bg-gray-800 text-white hover:bg-gray-700`;
    }
    return `${baseClass} text-gray-500 hover:text-gray-700 hover:bg-gray-50`;
  }

  // Получаем все медиа элементы (видео и изображения) для карусели
  getMediaItems(): Array<{ type: 'video' | 'image'; url: string; videoId?: string; title?: string; thumbnailUrl?: string | null }> {
    const items: Array<{ type: 'video' | 'image'; url: string; videoId?: string; title?: string; thumbnailUrl?: string | null }> = [];
    const videos = this.post().videos;
    const pictureUrls = this.post().pictureUrls;
    
    // Добавляем видео
    if (videos && videos.length > 0) {
      videos.forEach(video => {
        items.push({
          type: 'video',
          url: this.getVideoHlsUrl(video.id),
          videoId: video.id,
          title: video.title,
          thumbnailUrl: video.thumbnailUrl
        });
      });
    }
    
    // Добавляем изображения
    if (pictureUrls && pictureUrls.length > 0) {
      pictureUrls.forEach(imageUrl => {
        items.push({
          type: 'image',
          url: this.getPostImageUrl(imageUrl)
        });
      });
    }
    
    return items;
  }
}

