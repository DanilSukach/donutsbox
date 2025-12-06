import { Injectable, inject, signal, OnDestroy } from '@angular/core';
import { PostsFacade } from './posts-facade';
import { PostsRefresh } from '@app/core/services/posts-refresh.service';
import { SessionService } from '@app/core/services/session.service';
import * as signalR from '@microsoft/signalr';
import { environment } from '@env/environment';
import { Subscription } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class VideoStatusPollService implements OnDestroy {
  private postsFacade = inject(PostsFacade);
  private postsRefreshService = inject(PostsRefresh);
  private sessionService = inject(SessionService);

  private hubConnection?: signalR.HubConnection;
  private processingMediaIds = new Set<string>(); // Отслеживаем обрабатываемые видео и аудио
  private processingPosts = new Map<string, Set<string>>(); // postId -> Set<mediaId> для отслеживания постов с обрабатываемым медиа
  private currentPostId: string | null = null; // Сохраняем postId текущего обрабатываемого поста
  private subscriptionSub?: Subscription;
  
  readonly isPolling = signal(false);

  /**
   * Начать отслеживание статуса видео и аудио через SignalR после публикации поста
   * Автоматически обновит список постов когда весь медиа-контент обработан
   * Если отслеживание уже запущено, обновит список отслеживаемых медиа
   */
  startPollingAfterPublish(): void {
    // Инициализируем подключение к SignalR, если еще не подключены
    this.initSignalR();

    const wasPolling = this.isPolling();
    
    if (!wasPolling) {
      console.log('🔄 Запускаю отслеживание статуса медиа через SignalR...');
      this.isPolling.set(true);
      this.processingMediaIds.clear();
      this.processingPosts.clear();
      this.currentPostId = null;
    } else {
      console.log('🔄 Обновляю список отслеживаемых медиа...');
    }

    // Загружаем текущие посты для определения, какие медиа обрабатываются
    // Загружаем все посты (опубликованные и неопубликованные), так как медиа может обрабатываться в черновиках
    this.subscriptionSub?.unsubscribe();
    this.subscriptionSub = this.postsFacade.getMyPosts(1, 100, undefined).subscribe({
      next: (response) => {
        const posts = response.posts || [];
        let hasProcessingMedia = false;

        // Проверяем статус видео и аудио во всех постах
        posts.forEach((post: any) => {
          const postId = post.id;
          if (!postId) return;
          
          const videos = post.videos || [];
          const audios = post.audios || [];
          const postMedia = new Set<string>();
          
          videos.forEach((v: any) => {
            if (v.status === 'UPLOADED' || v.status === 'PROCESSING' || v.status === 'UPLOADING') {
              const mediaId = `video-${v.id}`;
              this.processingMediaIds.add(mediaId);
              postMedia.add(mediaId);
              hasProcessingMedia = true;
            }
          });
          
          audios.forEach((a: any) => {
            if (a.status === 'UPLOADED' || a.status === 'PROCESSING' || a.status === 'UPLOADING') {
              const mediaId = `audio-${a.id}`;
              this.processingMediaIds.add(mediaId);
              postMedia.add(mediaId);
              hasProcessingMedia = true;
            }
          });
          
          if (postMedia.size > 0) {
            // Объединяем с существующими медиа для этого поста, если отслеживание уже было запущено
            const existingMedia = this.processingPosts.get(postId);
            if (existingMedia) {
              postMedia.forEach(mediaId => existingMedia.add(mediaId));
              this.processingPosts.set(postId, existingMedia);
            } else {
              this.processingPosts.set(postId, postMedia);
            }
          }
        });

        if (!hasProcessingMedia && !wasPolling) {
          console.log('✅ Нет обрабатываемого медиа-контента');
          this.stopPolling();
        } else {
          console.log(`📊 Отслеживаем ${this.processingMediaIds.size} медиа-файлов через SignalR (${wasPolling ? 'обновлено' : 'новое отслеживание'})`);
        }
      },
      error: (err) => {
        console.error('❌ Ошибка загрузки постов:', err);
        if (!wasPolling) {
          this.stopPolling();
        }
      }
    });
  }

  private initSignalR(): void {
    if (this.hubConnection && this.hubConnection.state === signalR.HubConnectionState.Connected) {
      return; // Уже подключены
    }

    if (!this.hubConnection) {
      this.hubConnection = new signalR.HubConnectionBuilder()
        .withUrl(environment.mediaProcessingHubUrl, {
          withCredentials: true
        })
        .withAutomaticReconnect()
        .configureLogging(signalR.LogLevel.Information)
        .build();

      // Обработчик уведомления о готовности видео
      this.hubConnection.on('VideoProcessed', (data: { videoId: string; status: string; processedPath: string; postId?: string }) => {
        console.log('✅ Получено уведомление: видео обработано', data);
        const mediaId = `video-${data.videoId}`;
        this.processingMediaIds.delete(mediaId);
        
        // Сохраняем postId, если он передан
        if (data.postId) {
          this.currentPostId = data.postId;
          const postMedia = this.processingPosts.get(data.postId);
          if (postMedia) {
            postMedia.delete(mediaId);
            if (postMedia.size === 0) {
              this.processingPosts.delete(data.postId);
            }
          }
        }
        
        // Обновляем черновики сразу после обработки каждого медиа
        this.postsRefreshService.triggerRefresh();
        this.checkAllMediaProcessed(data.postId);
      });

      // Обработчик уведомления о готовности аудио
      this.hubConnection.on('AudioProcessed', (data: { audioId: string; status: string; processedPath: string; postId?: string }) => {
        console.log('✅ Получено уведомление: аудио обработано', data);
        const mediaId = `audio-${data.audioId}`;
        this.processingMediaIds.delete(mediaId);
        
        // Сохраняем postId, если он передан
        if (data.postId) {
          this.currentPostId = data.postId;
          const postMedia = this.processingPosts.get(data.postId);
          if (postMedia) {
            postMedia.delete(mediaId);
            if (postMedia.size === 0) {
              this.processingPosts.delete(data.postId);
            }
          }
        }
        
        // Обновляем черновики сразу после обработки каждого медиа
        this.postsRefreshService.triggerRefresh();
        this.checkAllMediaProcessed(data.postId);
      });

      this.hubConnection.onreconnecting(() => {
        console.log('🔄 Переподключение к SignalR...');
      });

      this.hubConnection.onreconnected(() => {
        console.log('✅ Переподключение к SignalR успешно');
        this.joinUserGroup();
      });

      this.hubConnection.onclose(() => {
        console.log('❌ Соединение с SignalR закрыто');
      });
    }

    if (this.hubConnection.state === signalR.HubConnectionState.Disconnected) {
      this.hubConnection.start()
        .then(() => {
          console.log('✅ Подключение к SignalR установлено');
          this.joinUserGroup();
        })
        .catch((err) => {
          console.error('❌ Ошибка подключения к SignalR:', err);
        });
    }
  }

  private joinUserGroup(): void {
    if (this.hubConnection && this.hubConnection.state === signalR.HubConnectionState.Connected) {
      this.hubConnection.invoke('JoinUserGroup')
        .then(() => {
          console.log('✅ Присоединен к группе пользователя для уведомлений о медиа');
        })
        .catch((err) => {
          console.error('❌ Ошибка присоединения к группе:', err);
        });
    }
  }

  private checkAllMediaProcessed(postId?: string): void {
    if (this.processingMediaIds.size === 0 && this.isPolling()) {
      // Находим postId, для которого все медиа обработано
      // Приоритет: переданный postId -> сохраненный currentPostId -> из processingPosts
      let publishedPostId = postId || this.currentPostId;
      
      // Если postId не найден, пытаемся найти его из processingPosts
      if (!publishedPostId) {
        if (this.processingPosts.size === 1) {
          publishedPostId = Array.from(this.processingPosts.keys())[0];
        } else if (this.processingPosts.size > 1) {
          // Если несколько постов, берем первый (должен быть только один активный)
          publishedPostId = Array.from(this.processingPosts.keys())[0];
          console.warn('⚠️ Несколько постов в обработке, используем первый:', publishedPostId);
        }
      }
      
      console.log('✅ Весь медиа-контент обработан!', {
        postId: publishedPostId,
        currentPostId: this.currentPostId,
        processingPosts: Array.from(this.processingPosts.keys()),
        processingMediaIds: Array.from(this.processingMediaIds)
      });
      
      this.stopPolling();
      
      // Уведомляем о публикации поста для локального удаления из черновиков
      if (publishedPostId) {
        console.log('📢 Отправка уведомления о публикации поста:', publishedPostId);
        this.postsRefreshService.notifyPostPublished(publishedPostId);
        // Очищаем сохраненный postId
        this.currentPostId = null;
      } else {
        console.warn('⚠️ Не удалось определить postId для публикации, просто обновляем');
        this.postsRefreshService.triggerRefresh();
      }
    } else {
      console.log(`⏳ Осталось обработать: ${this.processingMediaIds.size} медиа-файлов, постов: ${this.processingPosts.size}`);
    }
  }

  stopPolling(): void {
    console.log('⏹️ Остановка отслеживания');
    this.isPolling.set(false);
    this.processingMediaIds.clear();
    this.processingPosts.clear();
    this.currentPostId = null;
    this.subscriptionSub?.unsubscribe();
  }

  ngOnDestroy(): void {
    this.stopPolling();
    if (this.hubConnection) {
      this.hubConnection.stop().catch(err => {
        console.error('❌ Ошибка при остановке SignalR соединения:', err);
      });
    }
  }
}

