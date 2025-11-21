import { Component, inject, output, signal } from '@angular/core';
import { PostsFacade } from '../../services/posts-facade';
import { PostsRefresh } from '@app/core/services/posts-refresh.service';
import { VideoStatusPollService } from '../../services/video-status-poll.service';
import { FilesService } from '@app/api/donutsbox';

type ModalStep = 'create' | 'upload-video' | 'publish' | 'done';

interface UploadedVideo {
  videoId: string;
  title: string;
  file: File;
  thumbnailUrl?: string;
}

interface UploadedImage {
  imageId: string;
  title: string;
  file: File;
  key: string;
}

@Component({
  selector: 'app-create-post-modal',
  imports: [],
  templateUrl: './create-post-modal.html',
  styleUrl: './create-post-modal.css',
})
export class CreatePostModal {
  private postsFacade = inject(PostsFacade);
  private postsRefreshService = inject(PostsRefresh);
  private videoStatusPollService = inject(VideoStatusPollService);
  private filesService = inject(FilesService);

  readonly closed = output<void>();
  readonly published = output<void>();
  
  readonly currentStep = signal<ModalStep>('create');
  readonly isLoading = signal(false);
  readonly error = signal<string | null>(null);
  readonly wasPublished = signal(false);

  readonly postTitle = signal('');
  readonly postText = signal('');
  readonly postId = signal<string | null>(null);

  readonly videos = signal<UploadedVideo[]>([]);
  readonly videoTitle = signal('');
  readonly videoDescription = signal('');
  readonly selectedVideoFile = signal<File | null>(null);
  readonly selectedThumbnail = signal<File | null>(null);
  readonly isVideoFormExpanded = signal(false);

  readonly images = signal<UploadedImage[]>([]);
  readonly imageTitle = signal('');
  readonly selectedImageFiles = signal<File[]>([]);
  readonly isImageFormExpanded = signal(false);

  onVideoFileChange(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files[0]) {
      this.selectedVideoFile.set(input.files[0]);
    }
  }

  onThumbnailChange(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files[0]) {
      this.selectedThumbnail.set(input.files[0]);
    }
  }

  onImageFilesChange(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      const files = Array.from(input.files);
      // Ограничение до 8 файлов
      const limitedFiles = files.slice(0, 8);
      this.selectedImageFiles.set(limitedFiles);
      
      // Если выбрано больше 8, показываем предупреждение
      if (files.length > 8) {
        this.error.set(`Можно загрузить максимум 8 изображений. Выбрано ${limitedFiles.length}.`);
      } else {
        this.error.set(null);
      }
    }
  }

  createDraft(): void {
    if (!this.postTitle().trim() || !this.postText().trim()) {
      this.error.set('Заполните заголовок и текст поста');
      return;
    }

    this.isLoading.set(true);
    this.error.set(null);

    this.postsFacade
      .createDraft({
        title: this.postTitle(),
        text: this.postText(),
        pictureUrls: [],
        audioUrls: [],
      })
      .subscribe({
        next: (response) => {
          this.postId.set(response.postId!);
          this.currentStep.set('upload-video');
          this.isLoading.set(false);
        },
        error: (err) => {
          this.error.set(err.error?.message || 'Ошибка создания поста');
          this.isLoading.set(false);
        },
      });
  }

  uploadVideo(): void {
    const file = this.selectedVideoFile();
    const postId = this.postId();

    if (!file) {
      this.error.set('Выберите видео файл');
      return;
    }

    if (!postId) {
      this.error.set('Пост не создан');
      return;
    }

    if (!this.videoTitle().trim()) {
      this.error.set('Введите название видео');
      return;
    }

    this.isLoading.set(true);
    this.error.set(null);

    this.postsFacade
      .uploadVideo(
        postId,
        this.videoTitle(),
        file,
        this.videoDescription(),
        this.selectedThumbnail() || undefined
      )
      .subscribe({
        next: (response) => {
          if (!response.videoId) {
            this.error.set('Не получен ID видео от сервера');
            this.isLoading.set(false);
            return;
          }

          this.videos.update((vids) => [
            ...vids,
            {
              videoId: response.videoId!,
              title: this.videoTitle(),
              file: file,
              thumbnailUrl: response.thumbnailUrl || undefined,
            },
          ]);

          this.videoTitle.set('');
          this.videoDescription.set('');
          this.selectedVideoFile.set(null);
          this.selectedThumbnail.set(null);

          const videoInput = document.getElementById('video-file') as HTMLInputElement;
          const thumbInput = document.getElementById('thumbnail-file') as HTMLInputElement;
          if (videoInput) videoInput.value = '';
          if (thumbInput) thumbInput.value = '';

          this.isLoading.set(false);
        },
        error: (err) => {
          this.error.set(err.error?.message || 'Ошибка загрузки видео');
          this.isLoading.set(false);
        },
      });
  }

  uploadImages(): void {
    const files = this.selectedImageFiles();
    const postId = this.postId();

    if (!files || files.length === 0) {
      this.error.set('Выберите изображения');
      return;
    }

    if (files.length > 8) {
      this.error.set('Максимум 8 изображений');
      return;
    }

    if (!postId) {
      this.error.set('Пост не создан');
      return;
    }

    this.isLoading.set(true);
    this.error.set(null);

    this.filesService.apiFilesImagesPostPost(files, postId, this.imageTitle() || undefined)
      .subscribe({
        next: (response) => {
          if (!response || response.length === 0) {
            this.error.set('Изображения не загружены');
            this.isLoading.set(false);
            return;
          }

          // Добавляем загруженные изображения (фильтруем те, у которых есть key)
          const uploadedImages: UploadedImage[] = response
            .filter((item, index) => item.key && files[index])
            .map((item, index) => ({
              imageId: `img-${Date.now()}-${index}`,
              title: this.imageTitle(),
              file: files[index],
              key: item.key!,
            }));

          if (uploadedImages.length > 0) {
            this.images.update((imgs) => [...imgs, ...uploadedImages]);
          } else {
            this.error.set('Не удалось загрузить изображения');
          }

          // Очищаем форму
          this.imageTitle.set('');
          this.selectedImageFiles.set([]);
          const imageInput = document.getElementById('image-files') as HTMLInputElement;
          if (imageInput) imageInput.value = '';

          this.isLoading.set(false);
        },
        error: (err) => {
          this.error.set(err.error?.message || 'Ошибка загрузки изображений');
          this.isLoading.set(false);
        },
      });
  }

  proceedToPublish(): void {
    this.currentStep.set('publish');
  }

  publishPost(): void {
    const postId = this.postId();
    if (!postId) return;

    this.isLoading.set(true);
    this.error.set(null);

    this.postsFacade.publishPost(postId).subscribe({
      next: () => {
        this.currentStep.set('done');
        this.isLoading.set(false);
        console.log('✅ Пост опубликован');
        
        // Сразу обновляем список постов
        this.postsRefreshService.triggerRefresh();
        
        // Запускаем polling для автоматического обновления после обработки видео
        console.log('🎬 Запускаю polling статуса видео...');
        this.videoStatusPollService.startPollingAfterPublish();
      },
      error: (err) => {
        this.error.set(err.error?.message || 'Ошибка публикации поста');
        this.isLoading.set(false);
      },
    });
  }

  closeModal(): void {
    console.log('🚪 Закрытие модалки');
    this.closed.emit();
    this.resetModal();
  }

  private resetModal(): void {
    this.currentStep.set('create');
    this.postTitle.set('');
    this.postText.set('');
    this.postId.set(null);
    this.videos.set([]);
    this.videoTitle.set('');
    this.videoDescription.set('');
    this.selectedVideoFile.set(null);
    this.selectedThumbnail.set(null);
    this.isVideoFormExpanded.set(false);
    this.images.set([]);
    this.imageTitle.set('');
    this.selectedImageFiles.set([]);
    this.isImageFormExpanded.set(false);
    this.error.set(null);
    this.wasPublished.set(false);
  }

  goBack(): void {
    const step = this.currentStep();
    if (step === 'upload-video') {
      this.currentStep.set('create');
    } else if (step === 'publish') {
      this.currentStep.set('upload-video');
    }
  }
}