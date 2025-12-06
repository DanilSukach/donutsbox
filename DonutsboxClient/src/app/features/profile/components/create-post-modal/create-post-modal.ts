import { Component, OnInit, OnDestroy, inject, output, signal } from '@angular/core';
import { PostsFacade } from '../../services/posts-facade';
import { PostsRefresh } from '@app/core/services/posts-refresh.service';
import { VideoStatusPollService } from '../../services/video-status-poll.service';
import { CreateDraftRequestDto, FilesService, SubscriptionDto, AudioUploadResponseDto } from '@app/api/donutsbox';
import { HttpClient, HttpEvent, HttpEventType } from '@angular/common/http';
import { Subscription } from 'rxjs';
import { AudioRecorder } from '@app/shared/components/audio-recorder/audio-recorder';

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

interface UploadedAudio {
  audioId: string;
  title: string;
  file: File | Blob;
  status: string;
}

@Component({
  selector: 'app-create-post-modal',
  imports: [AudioRecorder],
  templateUrl: './create-post-modal.html',
  styleUrl: './create-post-modal.css',
})
export class CreatePostModal implements OnInit, OnDestroy {
  private postsFacade = inject(PostsFacade);
  private postsRefreshService = inject(PostsRefresh);
  private videoStatusPollService = inject(VideoStatusPollService);
  private filesService = inject(FilesService);
  private http = inject(HttpClient);

  readonly closed = output<void>();
  readonly published = output<void>();
  
  readonly currentStep = signal<ModalStep>('create');
  readonly isLoading = signal(false);
  readonly error = signal<string | null>(null);
  readonly wasPublished = signal(false);

  readonly postTitle = signal('');
  readonly postText = signal('');
  readonly postId = signal<string | null>(null);
  readonly audienceType = signal<'public' | 'subscribers'>('public');
  readonly availableSubscriptions = signal<SubscriptionDto[]>([]);
  readonly subscriptionsLoading = signal(false);
  readonly subscriptionsError = signal<string | null>(null);
  readonly selectedSubscriptionIds = signal<Set<string>>(new Set<string>());

  readonly videos = signal<UploadedVideo[]>([]);
  readonly videoTitle = signal('');
  readonly selectedVideoFile = signal<File | null>(null);
  readonly selectedThumbnail = signal<File | null>(null);
  readonly isVideoFormExpanded = signal(false);
  readonly uploadProgress = signal(0);
  readonly currentUploadingVideoId = signal<string | null>(null);

  private uploadSubscription: Subscription | null = null;
  private uploadAbortController: AbortController | null = null;

  readonly images = signal<UploadedImage[]>([]);
  readonly isImageFormExpanded = signal(false);
  readonly isUploadingImages = signal(false);

  readonly audios = signal<UploadedAudio[]>([]);
  readonly audioTitle = signal('');
  readonly selectedAudioFile = signal<File | null>(null);
  readonly isAudioFormExpanded = signal(false);
  readonly isRecordingAudio = signal(false);
  readonly isUploadingAudio = signal(false);
  readonly audioUploadProgress = signal(0);

  ngOnInit(): void {
    this.loadCreatorSubscriptions();
  }

  ngOnDestroy(): void {
    this.cancelUpload();
    this.uploadSubscription?.unsubscribe();
  }

  private loadCreatorSubscriptions(): void {
    this.subscriptionsLoading.set(true);
    this.subscriptionsError.set(null);

    this.postsFacade.getCreatorSubscriptions().subscribe({
      next: (subs) => {
        this.availableSubscriptions.set(subs);
        this.subscriptionsLoading.set(false);
      },
      error: () => {
        this.subscriptionsLoading.set(false);
        this.subscriptionsError.set('Не удалось загрузить подписки. Попробуйте обновить страницу.');
      }
    });
  }

  setAudience(type: 'public' | 'subscribers'): void {
    this.audienceType.set(type);
    if (type === 'public') {
      this.selectedSubscriptionIds.set(new Set<string>());
    }
  }

  toggleSubscription(subscriptionId: string | undefined): void {
    if (!subscriptionId) {
      return;
    }
    this.selectedSubscriptionIds.update((current) => {
      const next = new Set(current);
      if (next.has(subscriptionId)) {
        next.delete(subscriptionId);
      } else {
        next.add(subscriptionId);
      }
      return next;
    });
  }

  isSubscriptionSelected(subscriptionId: string | undefined): boolean {
    if (!subscriptionId) return false;
    return this.selectedSubscriptionIds().has(subscriptionId);
  }

  getAudienceDescription(): string {
    if (this.audienceType() === 'public') {
      return 'Пост увидят все пользователи (включая тех, кто без подписки).';
    }

    const selectedIds = this.getSelectedSubscriptionIds();
    if (selectedIds.length === 0) {
      return 'Выбран режим «Только подписчики». Необходимо выбрать хотя бы одну подписку.';
    }

    const titles = this.availableSubscriptions()
      .filter(sub => sub.id && selectedIds.includes(sub.id))
      .map(sub => sub.name)
      .filter(Boolean);

    if (titles.length === 0) {
      return 'Пост увидят подписчики выбранных тарифов.';
    }

    return `Пост увидят подписчики тарифов: ${titles.join(', ')}`;
  }

  getSelectedSubscriptionIds(): string[] {
    return Array.from(this.selectedSubscriptionIds()).map((id) => id);
  }

  private validateAudienceSelection(): boolean {
    if (this.audienceType() === 'subscribers' && this.selectedSubscriptionIds().size === 0) {
      this.error.set('Выберите хотя бы одну подписку, которая сможет видеть пост.');
      return false;
    }
    return true;
  }

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
      const currentCount = this.images().length;
      const maxAllowed = 8 - currentCount;
      
      if (maxAllowed <= 0) {
        this.error.set('Достигнут лимит в 8 изображений');
        input.value = '';
        return;
      }
      
      const limitedFiles = files.slice(0, maxAllowed);
      
      if (files.length > maxAllowed) {
        this.error.set(`Можно добавить ещё ${maxAllowed} изображений. Загружено ${limitedFiles.length}.`);
      } else {
        this.error.set(null);
      }
      
      // Сразу загружаем файлы
      this.uploadImagesImmediately(limitedFiles);
      input.value = '';
    }
  }

  private uploadImagesImmediately(files: File[]): void {
    const postId = this.postId();
    if (!postId || files.length === 0) return;

    this.isLoading.set(true);
    this.isUploadingImages.set(true);
    this.error.set(null);

    this.filesService.apiFilesImagesPostPost(files, postId)
      .subscribe({
        next: (response) => {
          if (!response || response.length === 0) {
            this.error.set('Изображения не загружены');
            this.isLoading.set(false);
            this.isUploadingImages.set(false);
            return;
          }

          const uploadedImages: UploadedImage[] = response
            .filter((item, index) => item.key && files[index])
            .map((item, index) => ({
              imageId: `img-${Date.now()}-${index}`,
              title: '',
              file: files[index],
              key: item.key!,
            }));

          if (uploadedImages.length > 0) {
            this.images.update((imgs) => [...imgs, ...uploadedImages]);
            this.isImageFormExpanded.set(false);
          } else {
            this.error.set('Не удалось загрузить изображения');
          }

          this.isLoading.set(false);
          this.isUploadingImages.set(false);
        },
        error: (err) => {
          this.error.set(err.error?.message || 'Ошибка загрузки изображений');
          this.isLoading.set(false);
          this.isUploadingImages.set(false);
        },
      });
  }

  createDraft(): void {
    if (!this.postTitle().trim()) {
      this.error.set('Заполните заголовок поста');
      return;
    }

    if (!this.validateAudienceSelection()) {
      return;
    }

    this.isLoading.set(true);
    this.error.set(null);

    const request: CreateDraftRequestDto = {
      title: this.postTitle(),
      text: this.postText(),
      pictureUrls: [],
      audioUrls: [],
      isPublic: this.audienceType() === 'public',
      subscriptionIds: this.audienceType() === 'subscribers' ? this.getSelectedSubscriptionIds() : []
    };

    this.postsFacade
      .createDraft(request)
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
    this.uploadProgress.set(0);

    // Используем XMLHttpRequest для отслеживания прогресса и возможности отмены
    const formData = new FormData();
    formData.append('ContentPostId', postId);
    formData.append('Title', this.videoTitle());
    formData.append('File', file);
    if (this.selectedThumbnail()) {
      formData.append('Thumbnail', this.selectedThumbnail()!);
    }

    const xhr = new XMLHttpRequest();
    this.uploadAbortController = new AbortController();
    xhr.withCredentials = true; // Отправляем cookies для авторизации
    
    xhr.upload.addEventListener('progress', (event) => {
      if (event.lengthComputable) {
        const progress = Math.round((event.loaded / event.total) * 100);
        this.uploadProgress.set(progress);
      }
    });

    xhr.addEventListener('load', () => {
      if (xhr.status >= 200 && xhr.status < 300) {
        try {
          const response = JSON.parse(xhr.responseText);
          if (!response.videoId) {
            this.error.set('Не получен ID видео от сервера');
            this.isLoading.set(false);
            this.uploadProgress.set(0);
            return;
          }

          this.currentUploadingVideoId.set(response.videoId);
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
          this.selectedVideoFile.set(null);
          this.selectedThumbnail.set(null);
          this.uploadProgress.set(0);
          this.currentUploadingVideoId.set(null);

          const videoInput = document.getElementById('video-file') as HTMLInputElement;
          const thumbInput = document.getElementById('thumbnail-file') as HTMLInputElement;
          if (videoInput) videoInput.value = '';
          if (thumbInput) thumbInput.value = '';

          this.isLoading.set(false);
          this.isVideoFormExpanded.set(false);
        } catch {
          this.error.set('Ошибка обработки ответа сервера');
          this.isLoading.set(false);
          this.uploadProgress.set(0);
        }
      } else {
        this.error.set('Ошибка загрузки видео');
        this.isLoading.set(false);
        this.uploadProgress.set(0);
      }
    });

    xhr.addEventListener('error', () => {
      this.error.set('Ошибка сети при загрузке видео');
      this.isLoading.set(false);
      this.uploadProgress.set(0);
    });

    xhr.addEventListener('abort', () => {
      this.error.set(null);
      this.isLoading.set(false);
      this.uploadProgress.set(0);
      this.selectedVideoFile.set(null);
      
      const videoInput = document.getElementById('video-file') as HTMLInputElement;
      if (videoInput) videoInput.value = '';
    });

    xhr.open('POST', '/api/Files/upload');
    xhr.send(formData);

    // Сохраняем ссылку на xhr для возможности отмены
    (this as Record<string, unknown>)['_currentXhr'] = xhr;
  }

  cancelUpload(): void {
    // Отменяем текущую загрузку
    const xhr = (this as Record<string, unknown>)['_currentXhr'] as XMLHttpRequest | undefined;
    if (xhr) {
      xhr.abort();
      (this as Record<string, unknown>)['_currentXhr'] = null;
    }
    
    // Если видео уже загружено и в обработке, отменяем обработку
    const videoId = this.currentUploadingVideoId();
    if (videoId) {
      this.http.post(`/api/CreatorPost/video/${videoId}/cancel`, {}).subscribe({
        next: () => {
          console.log('Обработка видео отменена');
        },
        error: (err: unknown) => {
          console.error('Ошибка отмены обработки видео:', err);
        }
      });
      this.currentUploadingVideoId.set(null);
    }
    
    this.isLoading.set(false);
    this.uploadProgress.set(0);
    this.selectedVideoFile.set(null);
    
    const videoInput = document.getElementById('video-file') as HTMLInputElement;
    if (videoInput) videoInput.value = '';
  }

  removeVideo(videoId: string): void {
    // Удаляем из локального списка
    this.videos.update((vids) => vids.filter((v) => v.videoId !== videoId));
    
    // Отправляем запрос на удаление на сервер
    this.http.delete(`/api/CreatorPost/video/${videoId}`).subscribe({
      next: () => {
        console.log('Видео удалено:', videoId);
      },
      error: (err: unknown) => {
        console.error('Ошибка удаления видео:', err);
      }
    });
  }

  removeImage(imageId: string): void {
    // Находим изображение чтобы получить ключ
    const image = this.images().find((img) => img.imageId === imageId);
    
    // Удаляем из локального списка
    this.images.update((imgs) => imgs.filter((img) => img.imageId !== imageId));
    
    // Отправляем запрос на удаление на сервер
    if (image?.key) {
      this.http.delete(`/api/CreatorPost/image/${encodeURIComponent(image.key)}`).subscribe({
        next: () => {
          console.log('Изображение удалено:', imageId);
        },
        error: (err: unknown) => {
          console.error('Ошибка удаления изображения:', err);
        }
      });
    }
  }

  proceedToPublish(): void {
    if (!this.validateAudienceSelection()) {
      return;
    }
    this.currentStep.set('publish');
  }

  publishPost(): void {
    const postId = this.postId();
    if (!postId) return;

    this.isLoading.set(true);
    this.error.set(null);

    const hasVideos = this.videos().length > 0;

    this.postsFacade.publishPost(postId).subscribe({
      next: () => {
        this.currentStep.set('done');
        this.isLoading.set(false);
        console.log('✅ Пост опубликован');
        
        const hasAudios = this.audios().length > 0;
        const hasVideos = this.videos().length > 0;
        
        if (hasVideos || hasAudios) {
          // Если есть видео или аудио - ждём обработки, не обновляем сразу
          // Плашка покажет что контент обрабатывается
          console.log('🎬 Запускаю polling статуса медиа (видео и/или аудио)...');
          this.videoStatusPollService.startPollingAfterPublish();
        } else {
          // Если только текст/изображения - обновляем сразу
          console.log('📝 Пост без медиа, обновляем сразу');
          this.postsRefreshService.triggerRefresh();
        }
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
    this.audienceType.set('public');
    this.selectedSubscriptionIds.set(new Set<string>());
    this.videos.set([]);
    this.videoTitle.set('');
    this.selectedVideoFile.set(null);
    this.selectedThumbnail.set(null);
    this.isVideoFormExpanded.set(false);
    this.uploadProgress.set(0);
    this.currentUploadingVideoId.set(null);
    this.images.set([]);
    this.isImageFormExpanded.set(false);
    this.isUploadingImages.set(false);
    this.audios.set([]);
    this.audioTitle.set('');
    this.selectedAudioFile.set(null);
    this.isAudioFormExpanded.set(false);
    this.isRecordingAudio.set(false);
    this.isUploadingAudio.set(false);
    this.audioUploadProgress.set(0);
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

  onAudioFileChange(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files[0]) {
      this.selectedAudioFile.set(input.files[0]);
    }
  }

  uploadAudioFile(): void {
    const file = this.selectedAudioFile();
    const postId = this.postId();

    if (!file) {
      this.error.set('Выберите аудио файл');
      return;
    }

    if (!postId) {
      this.error.set('Пост не создан');
      return;
    }

    if (!this.audioTitle().trim()) {
      this.error.set('Введите название аудио');
      return;
    }

    this.isLoading.set(true);
    this.isUploadingAudio.set(true);
    this.error.set(null);
    this.audioUploadProgress.set(0);

    // File уже является Blob, можно передать напрямую
    this.filesService
      .apiFilesAudioPost(postId, this.audioTitle(), file, 'events', true)
      .subscribe({
        next: (event: HttpEvent<AudioUploadResponseDto>) => {
          if (event.type === HttpEventType.UploadProgress) {
            // Обновляем прогресс загрузки
            if (event.total) {
              const progress = Math.round((event.loaded / event.total) * 100);
              this.audioUploadProgress.set(progress);
            }
          } else if (event.type === HttpEventType.Response) {
            // Загрузка завершена
            const response = event.body;
            if (!response?.audioId) {
              this.error.set('Не получен ID аудио от сервера');
              this.isLoading.set(false);
              this.isUploadingAudio.set(false);
              this.audioUploadProgress.set(0);
              return;
            }

            this.audios.update((auds) => [
              ...auds,
              {
                audioId: response.audioId!,
                title: this.audioTitle(),
                file: file,
                status: response.status || 'UPLOADING',
              },
            ]);

            this.audioTitle.set('');
            this.selectedAudioFile.set(null);
            this.audioUploadProgress.set(0);

            const audioInput = document.getElementById('audio-file') as HTMLInputElement;
            if (audioInput) audioInput.value = '';

            this.isLoading.set(false);
            this.isUploadingAudio.set(false);
            this.isAudioFormExpanded.set(false);
          }
        },
        error: (err) => {
          let errorMessage = 'Ошибка загрузки аудио';
          if (err.error?.message) {
            errorMessage = err.error.message;
          } else if (err.message) {
            errorMessage = err.message;
          }
          this.error.set(`Ошибка загрузки аудио: ${errorMessage}`);
          this.isLoading.set(false);
          this.isUploadingAudio.set(false);
          this.audioUploadProgress.set(0);
        },
      });
  }

  onRecordedAudio(blob: Blob): void {
    const postId = this.postId();
    if (!postId) {
      this.error.set('Пост не создан');
      return;
    }

    if (!this.audioTitle().trim()) {
      this.error.set('Введите название аудио');
      return;
    }

    // Конвертируем Blob в File для загрузки
    const fileName = `recording_${Date.now()}.webm`;
    const file = new File([blob], fileName, { type: blob.type || 'audio/webm' });
    
    this.selectedAudioFile.set(file);
    this.uploadAudioFile();
  }

  removeAudio(audioId: string): void {
    this.audios.update((auds) => auds.filter((a) => a.audioId !== audioId));
    
    // Отправляем запрос на удаление на сервер
    this.http.delete(`/api/Files/audio/${audioId}`).subscribe({
      next: () => {
        console.log('Аудио удалено:', audioId);
      },
      error: (err: unknown) => {
        console.error('Ошибка удаления аудио:', err);
        // Восстанавливаем аудио в списке при ошибке
        // (можно добавить уведомление пользователю)
      }
    });
  }

  getAudioFileSize(file: File | Blob): string {
    if (file && 'size' in file) {
      return (file.size / 1024 / 1024).toFixed(2);
    }
    return '0.00';
  }
}