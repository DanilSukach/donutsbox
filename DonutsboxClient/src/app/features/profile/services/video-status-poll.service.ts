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
   * Начать polling статуса видео после публикации поста
   * Автоматически обновит список постов когда все видео обработаны
   */
  startPollingAfterPublish(): void {
    if (this.isPolling()) {
      console.log('⏳ Polling уже запущен, пропускаем');
      return;
    }

    console.log('🔄 Запускаю polling статуса видео...');
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
          // Проверяем видео в статусах UPLOADED и PROCESSING
          return this.postsFacade.getMyVideos(1, 100);
        }),
        tap((response) => {
          const videos = response.videos || [];
          const processingVideos = videos.filter((v: any) => 
            v.status === 'UPLOADED' || v.status === 'PROCESSING'
          ).length;
          console.log(`  - Контент в обработке: ${processingVideos}`);

          // Если весь контент обработан
          if (processingVideos === 0) {
            console.log('✅ Весь контент обработан! Мягкое обновление постов...');
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

