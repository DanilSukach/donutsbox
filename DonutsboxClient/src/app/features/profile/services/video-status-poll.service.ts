import { Injectable, inject, signal } from '@angular/core';
import { interval, switchMap, takeWhile, tap } from 'rxjs';
import { PostsFacade } from './posts-facade';
import { PostsRefresh } from '@app/core/services/posts-refresh.service';

@Injectable({
  providedIn: 'root'
})
export class VideoStatusPollService {
  private postsFacade = inject(PostsFacade);
  private postsRefreshService = inject(PostsRefresh);

  private readonly pollingInterval = 5000; // Проверять каждые 5 секунд
  private readonly maxPollingAttempts = 60; // Максимум 5 минут (60 * 5 сек)
  
  readonly isPolling = signal(false);
  private pollingAttempts = 0;

  /**
   * Начать polling статуса видео и аудио после публикации поста
   * Автоматически обновит список постов когда весь медиа-контент обработан
   */
  startPollingAfterPublish(): void {
    if (this.isPolling()) {
      console.log('⏳ Polling уже запущен, пропускаем');
      return;
    }

    console.log('🔄 Запускаю polling статуса медиа (видео и аудио)...');
    this.isPolling.set(true);
    this.pollingAttempts = 0;

    interval(this.pollingInterval)
      .pipe(
        takeWhile(() => {
          this.pollingAttempts++;
          const shouldContinue = this.pollingAttempts <= this.maxPollingAttempts && this.isPolling();
          
          if (!shouldContinue) {
            console.log('⏹️ Остановка polling (достигнут лимит или остановлено вручную)');
            this.isPolling.set(false);
          }
          
          return shouldContinue;
        }),
        switchMap(() => {
          console.log(`🔍 Polling попытка ${this.pollingAttempts}/${this.maxPollingAttempts}...`);
          // Проверяем посты для получения статуса видео и аудио
          return this.postsFacade.getMyPosts(1, 100, true);
        }),
        tap((response) => {
          const posts = response.posts || [];
          let processingVideos = 0;
          let processingAudios = 0;

          // Проверяем статус видео и аудио во всех постах
          posts.forEach((post: any) => {
            const videos = post.videos || [];
            const audios = post.audios || [];
            
            processingVideos += videos.filter((v: any) => 
              v.status === 'UPLOADED' || v.status === 'PROCESSING' || v.status === 'UPLOADING'
            ).length;
            
            processingAudios += audios.filter((a: any) => 
              a.status === 'UPLOADED' || a.status === 'PROCESSING' || a.status === 'UPLOADING'
            ).length;
          });

          const totalProcessing = processingVideos + processingAudios;
          console.log(`  - Видео в обработке: ${processingVideos}, Аудио в обработке: ${processingAudios}, Всего: ${totalProcessing}`);

          // Если весь контент обработан
          if (totalProcessing === 0) {
            console.log('✅ Весь медиа-контент обработан! Мягкое обновление постов...');
            this.stopPolling();
            this.postsRefreshService.triggerRefresh();
          }
        })
      )
      .subscribe({
        error: (err) => {
          console.error('❌ Ошибка polling:', err);
          this.stopPolling();
        }
      });
  }

  stopPolling(): void {
    console.log('⏹️ Остановка polling');
    this.isPolling.set(false);
    this.pollingAttempts = 0;
  }
}

