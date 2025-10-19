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
          return this.postsFacade.getMyVideos(1, 100, 'UPLOADED'); // Проверяем видео в статусе UPLOADED
        }),
        tap((response) => {
          const uploadedVideos = response.videos?.length || 0;
          console.log(`  - Видео в обработке: ${uploadedVideos}`);

          // Если все видео обработаны (нет видео в статусе UPLOADED)
          if (uploadedVideos === 0) {
            console.log('✅ Все видео обработаны! Обновляю список постов...');
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

